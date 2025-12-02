<template>
  <transition name="fade">
    <section class="wrap">
      <div class="header">
        <h1>{{ userRole === 'Profesor' ? 'Mis Tareas Asignadas' : 'Mis Tareas' }}</h1>
        <p class="subtitle">
          {{ userRole === 'Profesor' 
            ? 'Todas las tareas que has creado en tus cursos' 
            : 'Todas tus tareas pendientes y completadas' 
          }}
        </p>
      </div>

      <div v-if="loading" class="loading">
        <div class="spinner"></div>
        Cargando tareas...
      </div>
      
      <div v-else-if="error" class="error-box">{{ error }}</div>
      
      <div v-else>
        <!-- Statistics -->
        <div class="stats-grid">
          <div class="stat-card total">
            <div class="stat-icon">📋</div>
            <div class="stat-info">
              <h3>{{ assignments.length }}</h3>
              <p>Total {{ userRole === 'Profesor' ? 'Creadas' : 'Tareas' }}</p>
            </div>
          </div>
          <div v-if="userRole === 'Estudiante'" class="stat-card completed">
            <div class="stat-icon">✅</div>
            <div class="stat-info">
              <h3>{{ completedCount }}</h3>
              <p>Completadas</p>
            </div>
          </div>
          <div v-if="userRole === 'Estudiante'" class="stat-card pending">
            <div class="stat-icon">⏳</div>
            <div class="stat-info">
              <h3>{{ pendingCount }}</h3>
              <p>Pendientes</p>
            </div>
          </div>
          <div v-if="userRole === 'Estudiante'" class="stat-card overdue">
            <div class="stat-icon">⚠️</div>
            <div class="stat-info">
              <h3>{{ overdueCount }}</h3>
              <p>Atrasadas</p>
            </div>
          </div>
          <div v-if="userRole === 'Profesor'" class="stat-card completed">
            <div class="stat-icon">👥</div>
            <div class="stat-info">
              <h3>{{ totalStudentCompletions }}</h3>
              <p>Entregas Totales</p>
            </div>
          </div>
        </div>

        <!-- Filters for students -->
        <div v-if="userRole === 'Estudiante'" class="filters">
          <button 
            @click="filter = 'all'" 
            :class="['filter-btn', { active: filter === 'all' }]"
          >
            Todas ({{ assignments.length }})
          </button>
          <button 
            @click="filter = 'pending'" 
            :class="['filter-btn', { active: filter === 'pending' }]"
          >
            Pendientes ({{ pendingCount }})
          </button>
          <button 
            @click="filter = 'completed'" 
            :class="['filter-btn', { active: filter === 'completed' }]"
          >
            Completadas ({{ completedCount }})
          </button>
          <button 
            @click="filter = 'overdue'" 
            :class="['filter-btn', { active: filter === 'overdue' }]"
          >
            Atrasadas ({{ overdueCount }})
          </button>
        </div>

        <!-- Empty state -->
        <div v-if="filteredAssignments.length === 0" class="empty">
          <div class="empty-icon">📭</div>
          <h3>No hay tareas</h3>
          <p v-if="userRole === 'Profesor'">
            Aún no has creado tareas. Ve a un curso específico para crear tareas.
          </p>
          <p v-else-if="filter === 'all'">
            No tienes tareas asignadas en este momento.
          </p>
          <p v-else-if="filter === 'pending'">
            ¡Genial! No tienes tareas pendientes.
          </p>
          <p v-else-if="filter === 'completed'">
            Aún no has completado ninguna tarea.
          </p>
          <p v-else-if="filter === 'overdue'">
            ¡Bien! No tienes tareas atrasadas.
          </p>
        </div>

        <!-- Assignments list -->
        <div v-else class="assignments-list">
          <article 
            v-for="item in filteredAssignments" 
            :key="item.assignment.id" 
            class="assignment-card"
            :class="{ 
              'is-completed': userRole === 'Estudiante' && item.completion?.status === 'completed',
              'is-overdue': isOverdue(item.assignment)
            }"
          >
            <div class="assignment-header">
              <div class="assignment-title-section">
                <h3>{{ item.assignment.title }}</h3>
                <span class="course-badge">{{ item.assignment.curso_codigo }}</span>
              </div>
              <div class="assignment-meta">
                <span class="type-badge" :class="item.assignment.assignment_type">
                  {{ getTypeLabel(item.assignment.assignment_type) }}
                </span>
              </div>
            </div>

            <p class="assignment-description">{{ item.assignment.description }}</p>

            <div class="assignment-course">
              <span class="course-icon">📚</span>
              <span>{{ item.assignment.curso_nombre }}</span>
            </div>

            <div class="assignment-details">
              <div class="detail-item">
                <span class="detail-icon">📅</span>
                <span class="detail-label">Fecha límite:</span>
                <span class="detail-value" :class="{ 'text-danger': isOverdue(item.assignment) }">
                  {{ item.assignment.due_date ? formatDate(item.assignment.due_date) : 'Sin fecha límite' }}
                </span>
              </div>
              <div v-if="item.assignment.points" class="detail-item">
                <span class="detail-icon">🎯</span>
                <span class="detail-label">Puntos:</span>
                <span class="detail-value">{{ item.assignment.points }}</span>
              </div>
            </div>

            <!-- Student view: completion status -->
            <div v-if="userRole === 'Estudiante'" class="assignment-status">
              <div v-if="item.completion?.status === 'completed' || item.completion?.status === 'completed_with_errors'" class="status-completed">
                <span class="status-icon">✅</span>
                <span>Completada el {{ formatDate(item.completion.completed_at) }}</span>
                <span v-if="item.completion?.status === 'completed_with_errors'" class="status-warning">⚠️ Con errores</span>
              </div>
              <div v-else class="status-pending">
                <button 
                  @click="openSubmitModal(item.assignment.id)" 
                  class="btn-complete"
                >
                  📤 Entregar Tarea
                </button>
              </div>
            </div>

            <!-- Teacher view: completion stats -->
            <div v-if="userRole === 'Profesor'" class="assignment-stats">
              <div class="stat-item">
                <span class="stat-label">Entregas:</span>
                <span class="stat-value">
                  {{ item.completions?.completed || 0 }} / {{ item.completions?.total || 0 }}
                </span>
              </div>
              <div class="progress-bar">
                <div 
                  class="progress-fill" 
                  :style="{ width: getCompletionPercentage(item.completions) + '%' }"
                ></div>
              </div>
            </div>

            <div class="assignment-actions">
              <button 
                @click="goToCourse(item.assignment.curso_id)" 
                class="btn-secondary"
              >
                <span class="btn-icon">📚</span>
                Ver Curso
              </button>
              <button 
                @click="goToAssignment(item.assignment.curso_id, item.assignment.id)" 
                class="btn-primary"
              >
                <span class="btn-icon">👁️</span>
                Ver Detalles
              </button>
            </div>
          </article>
        </div>
      </div>

      <!-- Submit Assignment Modal -->
      <transition name="modal-fade">
        <div v-if="showSubmitModal" class="modal-overlay" @click="closeSubmitModal">
          <div class="modal-content" @click.stop>
            <div class="modal-header">
              <h2>📤 Entregar Tarea</h2>
              <button @click="closeSubmitModal" class="modal-close">✕</button>
            </div>
            
            <div class="modal-body">
              <!-- File Upload Area -->
              <div class="upload-section">
                <label class="upload-label">Archivos</label>
                <div 
                  class="upload-dropzone"
                  :class="{ 'dragging': isDragging }"
                  @drop.prevent="handleDrop"
                  @dragover.prevent="isDragging = true"
                  @dragleave.prevent="isDragging = false"
                >
                  <input 
                    type="file" 
                    ref="fileInput"
                    @change="handleFileSelect"
                    multiple
                    style="display: none"
                  />
                  <div class="upload-icon">📁</div>
                  <p class="upload-text">
                    Arrastra archivos aquí o 
                    <button type="button" @click="$refs.fileInput.click()" class="upload-browse">
                      examinar
                    </button>
                  </p>
                  <p class="upload-hint">Puedes subir código (.py, .js, .java, etc.) y otros documentos</p>
                </div>

                <!-- File List -->
                <div v-if="selectedFiles.length > 0" class="file-list">
                  <div v-for="(file, index) in selectedFiles" :key="index" class="file-item">
                    <span class="file-icon">{{ getFileIcon(file.name) }}</span>
                    <div class="file-info">
                      <span class="file-name">{{ file.name }}</span>
                      <span class="file-size">{{ formatFileSize(file.size) }}</span>
                      <span v-if="isCodeFile(file.name)" class="code-badge">📝 Código</span>
                    </div>
                    <button @click="removeFile(index)" class="file-remove">✕</button>
                  </div>
                </div>
              </div>

              <!-- Notes Section -->
              <div class="notes-section">
                <label for="notes" class="notes-label">Notas o comentarios (opcional)</label>
                <textarea 
                  id="notes"
                  v-model="submissionNotes"
                  class="notes-textarea"
                  rows="3"
                  placeholder="Agrega cualquier comentario sobre tu entrega..."
                ></textarea>
              </div>

              <!-- Analysis Preview -->
              <div v-if="codeFileCount > 0" class="analysis-info">
                <div class="info-icon">🔍</div>
                <p>
                  Se encontraron <strong>{{ codeFileCount }}</strong> archivo(s) de código.
                  Se analizarán automáticamente al entregar.
                </p>
              </div>

              <!-- Submission Results -->
              <div v-if="submissionResult" class="submission-result">
                <div v-if="submissionResult.success" class="result-success">
                  <div class="result-icon">✅</div>
                  <div class="result-content">
                    <h3>Tarea entregada exitosamente</h3>
                    <p>Archivos procesados: {{ submissionResult.filesProcessed }}</p>
                    <p v-if="submissionResult.codeFilesAnalyzed > 0">
                      Archivos de código analizados: {{ submissionResult.codeFilesAnalyzed }}
                    </p>
                    <div v-if="submissionResult.codeAnalysis && submissionResult.codeAnalysis.length > 0" class="analysis-summary">
                      <h4>Resultados del análisis:</h4>
                      <div v-for="(analysis, idx) in submissionResult.codeAnalysis" :key="idx" class="analysis-item">
                        <span class="analysis-file">{{ analysis.fileName }}</span>
                        <span :class="['analysis-status', analysis.status]">
                          {{ analysis.status === 'analyzed' ? '✓ Analizado' : '⚠️ Error' }}
                        </span>
                      </div>
                    </div>
                  </div>
                </div>
                <div v-else class="result-error">
                  <div class="result-icon">❌</div>
                  <div class="result-content">
                    <h3>Error al entregar</h3>
                    <p>{{ submissionResult.error }}</p>
                  </div>
                </div>
              </div>
            </div>

            <div class="modal-footer">
              <button @click="closeSubmitModal" class="btn-cancel" :disabled="submitting">
                Cancelar
              </button>
              <button 
                @click="submitAssignment" 
                class="btn-submit"
                :disabled="submitting || (selectedFiles.length === 0 && !submissionNotes)"
              >
                {{ submitting ? '⏳ Enviando...' : '📤 Entregar' }}
              </button>
            </div>
          </div>
        </div>
      </transition>
    </section>
  </transition>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { supabase } from '../supabase'

const router = useRouter()

const assignments = ref([])
const userRole = ref('')
const loading = ref(true)
const error = ref(null)
const completing = ref(null)
const filter = ref('all')

// Submit modal state
const showSubmitModal = ref(false)
const currentAssignmentId = ref(null)
const selectedFiles = ref([])
const submissionNotes = ref('')
const isDragging = ref(false)
const submitting = ref(false)
const submissionResult = ref(null)
const fileInput = ref(null)

const TAREAS_API = import.meta.env.VITE_TAREAS_API || 'http://localhost:5011'

const codeFileExtensions = ['.py', '.js', '.ts', '.jsx', '.tsx', '.java', '.cpp', '.c', '.cs', '.html', '.css']

const codeFileCount = computed(() => {
  return selectedFiles.value.filter(file => isCodeFile(file.name)).length
})

// Computed properties
const completedCount = computed(() => {
  return assignments.value.filter(item => 
    item.completion?.status === 'completed'
  ).length
})

const pendingCount = computed(() => {
  return assignments.value.filter(item => 
    !item.completion || item.completion.status !== 'completed'
  ).length
})

const overdueCount = computed(() => {
  return assignments.value.filter(item => 
    isOverdue(item.assignment) && 
    (!item.completion || item.completion.status !== 'completed')
  ).length
})

const totalStudentCompletions = computed(() => {
  return assignments.value.reduce((sum, item) => {
    return sum + (item.completions?.completed || 0)
  }, 0)
})

const filteredAssignments = computed(() => {
  if (userRole.value !== 'Estudiante' || filter.value === 'all') {
    return assignments.value
  }

  if (filter.value === 'completed') {
    return assignments.value.filter(item => 
      item.completion?.status === 'completed'
    )
  }

  if (filter.value === 'pending') {
    return assignments.value.filter(item => 
      !item.completion || item.completion.status !== 'completed'
    )
  }

  if (filter.value === 'overdue') {
    return assignments.value.filter(item => 
      isOverdue(item.assignment) && 
      (!item.completion || item.completion.status !== 'completed')
    )
  }

  return assignments.value
})

// Methods
function formatDate(dateString) {
  if (!dateString) return ''
  return new Date(dateString).toLocaleDateString('es-ES', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

function getTypeLabel(type) {
  const types = {
    'tarea': 'Tarea',
    'examen': 'Examen',
    'proyecto': 'Proyecto',
    'quiz': 'Quiz',
    'presentacion': 'Presentación'
  }
  return types[type] || type
}

function isOverdue(assignment) {
  if (!assignment.due_date) return false
  return new Date(assignment.due_date) < new Date()
}

function getCompletionPercentage(completions) {
  if (!completions || completions.total === 0) return 0
  return Math.round((completions.completed / completions.total) * 100)
}

async function fetchAssignments() {
  try {
    loading.value = true
    error.value = null

    const { data: { session } } = await supabase.auth.getSession()
    if (!session) {
      error.value = 'No estás autenticado'
      return
    }

    const response = await fetch(`${TAREAS_API}/api/assignments`, {
      headers: {
        'Authorization': `Bearer ${session.access_token}`,
        'X-User-Email': session.user.email
      }
    })

    if (response.ok) {
      const data = await response.json()
      assignments.value = data.assignments || []
      userRole.value = data.userRole || 'Estudiante'
    } else {
      const data = await response.json()
      error.value = data.detail || 'Error al cargar tareas'
    }
  } catch (e) {
    console.error('Error fetching assignments:', e)
    error.value = 'Error de conexión al cargar tareas'
  } finally {
    loading.value = false
  }
}

// Modal functions
function openSubmitModal(assignmentId) {
  currentAssignmentId.value = assignmentId
  showSubmitModal.value = true
  selectedFiles.value = []
  submissionNotes.value = ''
  submissionResult.value = null
}

function closeSubmitModal() {
  if (!submitting.value) {
    showSubmitModal.value = false
    currentAssignmentId.value = null
    selectedFiles.value = []
    submissionNotes.value = ''
    submissionResult.value = null
  }
}

function handleFileSelect(event) {
  const files = Array.from(event.target.files)
  selectedFiles.value = [...selectedFiles.value, ...files]
  event.target.value = '' // Reset input
}

function handleDrop(event) {
  isDragging.value = false
  const files = Array.from(event.dataTransfer.files)
  selectedFiles.value = [...selectedFiles.value, ...files]
}

function removeFile(index) {
  selectedFiles.value.splice(index, 1)
}

function isCodeFile(filename) {
  const ext = filename.substring(filename.lastIndexOf('.')).toLowerCase()
  return codeFileExtensions.includes(ext)
}

function getFileIcon(filename) {
  if (isCodeFile(filename)) return '💻'
  const ext = filename.substring(filename.lastIndexOf('.')).toLowerCase()
  if (['.pdf'].includes(ext)) return '📄'
  if (['.doc', '.docx'].includes(ext)) return '📝'
  if (['.zip', '.rar'].includes(ext)) return '📦'
  if (['.png', '.jpg', '.jpeg', '.gif'].includes(ext)) return '🖼️'
  return '📎'
}

function formatFileSize(bytes) {
  if (bytes === 0) return '0 Bytes'
  const k = 1024
  const sizes = ['Bytes', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i]
}

async function submitAssignment() {
  if (submitting.value) return
  
  try {
    submitting.value = true
    submissionResult.value = null

    const { data: { session } } = await supabase.auth.getSession()
    if (!session) {
      submissionResult.value = { success: false, error: 'No estás autenticado' }
      return
    }

    const formData = new FormData()
    
    // Add files
    selectedFiles.value.forEach(file => {
      formData.append('files', file)
    })
    
    // Add notes
    if (submissionNotes.value) {
      formData.append('notes', submissionNotes.value)
    }

    const response = await fetch(
      `${TAREAS_API}/api/assignments/${currentAssignmentId.value}/complete`,
      {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${session.access_token}`,
          'X-User-Email': session.user.email
        },
        body: formData
      }
    )

    if (response.ok) {
      const data = await response.json()
      submissionResult.value = {
        success: true,
        filesProcessed: data.filesProcessed,
        codeFilesAnalyzed: data.codeFilesAnalyzed,
        codeAnalysis: data.codeAnalysis,
        status: data.status
      }

      // Update local state
      const assignment = assignments.value.find(a => a.assignment.id === currentAssignmentId.value)
      if (assignment) {
        assignment.completion = {
          completed_at: new Date().toISOString(),
          status: data.status
        }
      }

      // Close modal after 3 seconds on success
      setTimeout(() => {
        closeSubmitModal()
      }, 3000)
    } else {
      const data = await response.json()
      submissionResult.value = {
        success: false,
        error: data.detail || data.title || 'Error al entregar tarea'
      }
    }
  } catch (e) {
    console.error('Error submitting assignment:', e)
    submissionResult.value = {
      success: false,
      error: 'Error de conexión al entregar tarea'
    }
  } finally {
    submitting.value = false
  }
}

function goToCourse(cursoId) {
  router.push(`/courses/${cursoId}`)
}

function goToAssignment(cursoId, assignmentId) {
  router.push(`/courses/${cursoId}/assignments`)
}

onMounted(() => {
  fetchAssignments()
})
</script>

<style scoped>
.wrap {
  max-width: 1200px;
  margin: 2rem auto;
  padding: 0 1rem;
  animation: slideIn 0.7s;
}

.header {
  margin-bottom: 2rem;
}

.header h1 {
  margin: 0 0 0.5rem 0;
  color: #2a4dd0;
}

.subtitle {
  color: #666;
  margin: 0;
}

/* Loading & Error */
.loading {
  text-align: center;
  padding: 3rem;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
}

.spinner {
  width: 40px;
  height: 40px;
  border: 4px solid #f3f4f6;
  border-top-color: #2a4dd0;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.error-box {
  background: #fee2e2;
  color: #991b1b;
  padding: 1rem;
  border-radius: 8px;
  text-align: center;
}

/* Statistics */
.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1rem;
  margin-bottom: 2rem;
}

.stat-card {
  background: white;
  border-radius: 12px;
  padding: 1.5rem;
  box-shadow: 0 2px 8px rgba(0,0,0,0.06);
  display: flex;
  align-items: center;
  gap: 1rem;
  transition: transform 0.2s, box-shadow 0.2s;
}

.stat-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0,0,0,0.1);
}

.stat-icon {
  font-size: 2rem;
}

.stat-info h3 {
  margin: 0;
  font-size: 2rem;
  color: #2a4dd0;
}

.stat-info p {
  margin: 0;
  color: #666;
  font-size: 0.9rem;
}

.stat-card.total { border-left: 4px solid #2a4dd0; }
.stat-card.completed { border-left: 4px solid #10b981; }
.stat-card.pending { border-left: 4px solid #f59e0b; }
.stat-card.overdue { border-left: 4px solid #ef4444; }

/* Filters */
.filters {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 2rem;
  flex-wrap: wrap;
}

.filter-btn {
  padding: 0.6rem 1.2rem;
  border: 2px solid #e5e7eb;
  background: white;
  border-radius: 8px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
}

.filter-btn:hover {
  border-color: #2a4dd0;
  background: #eef2ff;
}

.filter-btn.active {
  background: #2a4dd0;
  color: white;
  border-color: #2a4dd0;
}

/* Empty state */
.empty {
  text-align: center;
  padding: 3rem;
  background: #f9fafb;
  border-radius: 12px;
}

.empty-icon {
  font-size: 4rem;
  margin-bottom: 1rem;
}

.empty h3 {
  color: #2a4dd0;
  margin-bottom: 0.5rem;
}

.empty p {
  color: #666;
  margin: 0;
}

/* Assignments list */
.assignments-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.assignment-card {
  background: white;
  border-radius: 12px;
  padding: 1.5rem;
  box-shadow: 0 2px 8px rgba(0,0,0,0.06);
  transition: transform 0.2s, box-shadow 0.2s;
  border-left: 4px solid #2a4dd0;
}

.assignment-card:hover {
  transform: translateX(4px);
  box-shadow: 0 4px 12px rgba(0,0,0,0.1);
}

.assignment-card.is-completed {
  border-left-color: #10b981;
  opacity: 0.85;
}

.assignment-card.is-overdue {
  border-left-color: #ef4444;
}

.assignment-header {
  display: flex;
  justify-content: space-between;
  align-items: start;
  margin-bottom: 1rem;
  gap: 1rem;
}

.assignment-title-section {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.assignment-title-section h3 {
  margin: 0;
  color: #2a4dd0;
  font-size: 1.2rem;
}

.course-badge {
  background: #eef2ff;
  color: #2a4dd0;
  padding: 0.25rem 0.6rem;
  border-radius: 6px;
  font-size: 0.85rem;
  font-weight: 600;
}

.type-badge {
  padding: 0.4rem 0.8rem;
  border-radius: 6px;
  font-size: 0.85rem;
  font-weight: 600;
  text-transform: uppercase;
}

.type-badge.tarea { background: #dbeafe; color: #1e40af; }
.type-badge.examen { background: #fee2e2; color: #991b1b; }
.type-badge.proyecto { background: #d1fae5; color: #065f46; }
.type-badge.quiz { background: #fef3c7; color: #92400e; }
.type-badge.presentacion { background: #e0e7ff; color: #3730a3; }

.assignment-description {
  color: #666;
  margin-bottom: 1rem;
  line-height: 1.6;
}

.assignment-course {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem;
  background: #f9fafb;
  border-radius: 8px;
  margin-bottom: 1rem;
  font-weight: 500;
}

.course-icon {
  font-size: 1.2rem;
}

.assignment-details {
  display: flex;
  flex-wrap: wrap;
  gap: 1.5rem;
  margin-bottom: 1rem;
}

.detail-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.9rem;
}

.detail-icon {
  font-size: 1.1rem;
}

.detail-label {
  color: #666;
}

.detail-value {
  font-weight: 600;
  color: #2a4dd0;
}

.text-danger {
  color: #ef4444 !important;
}

/* Status */
.assignment-status {
  margin-bottom: 1rem;
}

.status-completed {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem;
  background: #d1fae5;
  color: #065f46;
  border-radius: 8px;
  font-weight: 500;
}

.status-icon {
  font-size: 1.2rem;
}

.status-pending {
  display: flex;
  justify-content: flex-start;
}

.btn-complete {
  background: #10b981;
  color: white;
  border: none;
  padding: 0.75rem 1.5rem;
  border-radius: 8px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-complete:hover:not(:disabled) {
  background: #059669;
  transform: translateY(-1px);
}

.btn-complete:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

/* Teacher stats */
.assignment-stats {
  background: #f9fafb;
  padding: 1rem;
  border-radius: 8px;
  margin-bottom: 1rem;
}

.stat-item {
  display: flex;
  justify-content: space-between;
  margin-bottom: 0.5rem;
}

.stat-label {
  color: #666;
  font-weight: 500;
}

.stat-value {
  font-weight: 700;
  color: #2a4dd0;
}

.progress-bar {
  width: 100%;
  height: 8px;
  background: #e5e7eb;
  border-radius: 4px;
  overflow: hidden;
}

.progress-fill {
  height: 100%;
  background: #10b981;
  transition: width 0.3s ease;
}

/* Actions */
.assignment-actions {
  display: flex;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.btn-primary, .btn-secondary {
  padding: 0.6rem 1.2rem;
  border-radius: 8px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
  border: none;
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
}

.btn-primary {
  background: #2a4dd0;
  color: white;
}

.btn-primary:hover {
  background: #1e3a8a;
  transform: translateY(-1px);
}

.btn-secondary {
  background: white;
  color: #2a4dd0;
  border: 2px solid #2a4dd0;
}

.btn-secondary:hover {
  background: #eef2ff;
}

.btn-icon {
  font-size: 1rem;
}

/* Animations */
@keyframes slideIn {
  from {
    transform: translateY(20px);
    opacity: 0;
  }
  to {
    transform: translateY(0);
    opacity: 1;
  }
}

.fade-enter-active, .fade-leave-active {
  transition: opacity 0.3s;
}

.fade-enter-from, .fade-leave-to {
  opacity: 0;
}

/* Modal Styles */
.modal-fade-enter-active, .modal-fade-leave-active {
  transition: opacity 0.3s;
}

.modal-fade-enter-from, .modal-fade-leave-to {
  opacity: 0;
}

.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.6);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 1rem;
}

.modal-content {
  background: white;
  border-radius: 16px;
  max-width: 600px;
  width: 100%;
  max-height: 90vh;
  overflow-y: auto;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.5rem;
  border-bottom: 1px solid #e5e7eb;
}

.modal-header h2 {
  margin: 0;
  font-size: 1.5rem;
  color: #1f2937;
}

.modal-close {
  background: none;
  border: none;
  font-size: 1.5rem;
  color: #6b7280;
  cursor: pointer;
  padding: 0.5rem;
  line-height: 1;
  border-radius: 4px;
  transition: background 0.2s;
}

.modal-close:hover {
  background: #f3f4f6;
}

.modal-body {
  padding: 1.5rem;
}

.upload-section {
  margin-bottom: 1.5rem;
}

.upload-label {
  display: block;
  font-weight: 600;
  margin-bottom: 0.5rem;
  color: #374151;
}

.upload-dropzone {
  border: 2px dashed #d1d5db;
  border-radius: 12px;
  padding: 2rem;
  text-align: center;
  background: #f9fafb;
  transition: all 0.2s;
  cursor: pointer;
}

.upload-dropzone:hover, .upload-dropzone.dragging {
  border-color: #2a4dd0;
  background: #eff6ff;
}

.upload-icon {
  font-size: 3rem;
  margin-bottom: 0.5rem;
}

.upload-text {
  margin: 0.5rem 0;
  color: #6b7280;
}

.upload-browse {
  background: none;
  border: none;
  color: #2a4dd0;
  font-weight: 600;
  cursor: pointer;
  text-decoration: underline;
}

.upload-hint {
  margin: 0.5rem 0 0 0;
  font-size: 0.875rem;
  color: #9ca3af;
}

.file-list {
  margin-top: 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.file-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem;
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
}

.file-icon {
  font-size: 1.5rem;
}

.file-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.file-name {
  font-weight: 500;
  color: #374151;
  font-size: 0.875rem;
}

.file-size {
  font-size: 0.75rem;
  color: #6b7280;
}

.code-badge {
  display: inline-block;
  background: #dbeafe;
  color: #1e40af;
  padding: 0.125rem 0.5rem;
  border-radius: 4px;
  font-size: 0.75rem;
  font-weight: 600;
  margin-top: 0.25rem;
  width: fit-content;
}

.file-remove {
  background: #fee2e2;
  border: none;
  color: #991b1b;
  width: 24px;
  height: 24px;
  border-radius: 50%;
  cursor: pointer;
  font-size: 0.875rem;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.2s;
}

.file-remove:hover {
  background: #fecaca;
}

.notes-section {
  margin-bottom: 1.5rem;
}

.notes-label {
  display: block;
  font-weight: 600;
  margin-bottom: 0.5rem;
  color: #374151;
}

.notes-textarea {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  font-family: inherit;
  font-size: 0.875rem;
  resize: vertical;
  transition: border-color 0.2s;
}

.notes-textarea:focus {
  outline: none;
  border-color: #2a4dd0;
  box-shadow: 0 0 0 3px rgba(42, 77, 208, 0.1);
}

.analysis-info {
  background: #eff6ff;
  border: 1px solid #bfdbfe;
  border-radius: 8px;
  padding: 1rem;
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 1rem;
}

.info-icon {
  font-size: 1.5rem;
}

.analysis-info p {
  margin: 0;
  color: #1e40af;
  font-size: 0.875rem;
}

.submission-result {
  margin-top: 1rem;
  border-radius: 8px;
  overflow: hidden;
}

.result-success, .result-error {
  display: flex;
  gap: 1rem;
  padding: 1rem;
}

.result-success {
  background: #d1fae5;
  border: 1px solid #6ee7b7;
}

.result-error {
  background: #fee2e2;
  border: 1px solid #fecaca;
}

.result-icon {
  font-size: 2rem;
}

.result-content {
  flex: 1;
}

.result-content h3 {
  margin: 0 0 0.5rem 0;
  font-size: 1rem;
}

.result-content p {
  margin: 0.25rem 0;
  font-size: 0.875rem;
}

.result-success h3 {
  color: #065f46;
}

.result-success p {
  color: #047857;
}

.result-error h3 {
  color: #991b1b;
}

.result-error p {
  color: #b91c1c;
}

.analysis-summary {
  margin-top: 0.75rem;
  padding-top: 0.75rem;
  border-top: 1px solid #a7f3d0;
}

.analysis-summary h4 {
  margin: 0 0 0.5rem 0;
  font-size: 0.875rem;
  color: #065f46;
}

.analysis-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.5rem;
  background: white;
  border-radius: 4px;
  margin-bottom: 0.25rem;
}

.analysis-file {
  font-size: 0.875rem;
  color: #374151;
  font-weight: 500;
}

.analysis-status {
  font-size: 0.75rem;
  font-weight: 600;
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
}

.analysis-status.analyzed {
  background: #d1fae5;
  color: #065f46;
}

.analysis-status.error {
  background: #fee2e2;
  color: #991b1b;
}

.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: 0.75rem;
  padding: 1.5rem;
  border-top: 1px solid #e5e7eb;
}

.btn-cancel, .btn-submit {
  padding: 0.75rem 1.5rem;
  border-radius: 8px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
  border: none;
  font-size: 0.875rem;
}

.btn-cancel {
  background: #f3f4f6;
  color: #374151;
}

.btn-cancel:hover:not(:disabled) {
  background: #e5e7eb;
}

.btn-submit {
  background: #2a4dd0;
  color: white;
}

.btn-submit:hover:not(:disabled) {
  background: #1e3a8a;
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(42, 77, 208, 0.3);
}

.btn-cancel:disabled, .btn-submit:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.status-warning {
  margin-left: 0.5rem;
  font-size: 0.875rem;
  color: #f59e0b;
}

/* Responsive */
@media (max-width: 768px) {
  .stats-grid {
    grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
  }

  .assignment-header {
    flex-direction: column;
  }

  .assignment-actions {
    flex-direction: column;
  }

  .btn-primary, .btn-secondary {
    width: 100%;
    justify-content: center;
  }

  .modal-content {
    max-width: 100%;
    margin: 0.5rem;
  }

  .modal-footer {
    flex-direction: column;
  }

  .btn-cancel, .btn-submit {
    width: 100%;
  }
}
</style>
