<template>
  <transition name="fade">
    <section class="wrap">
      <!-- Header para vista general de tareas -->
      <div class="header" v-if="!courseId">
        <button @click="$router.back()" class="btn-back">← Volver</button>
        <h1>Mis Tareas</h1>
        <button 
          v-if="userRole === 'Profesor' && hasAssignedCourses" 
          @click="showCourseSelectModal = true" 
          class="btn-primary"
        >
          ➕ Nueva Tarea
        </button>
        <button 
          v-else-if="userRole === 'Profesor' && !hasAssignedCourses" 
          class="btn-primary"
          disabled
          title="No tienes cursos asignados"
        >
          ➕ Nueva Tarea (Sin Cursos)
        </button>
        
        <!-- Botón de diagnóstico -->
        <button @click="runFullDiagnostics" class="btn-diagnostic" :disabled="runningDiagnostics">
          {{ runningDiagnostics ? '🔍 Ejecutando...' : '🔧 Diagnóstico' }}
        </button>
      </div>

      <!-- Header para tareas de un curso específico -->
      <div class="header" v-else>
        <button @click="$router.back()" class="btn-back">← Volver al Curso</button>
        <h1>Tareas - {{ curso?.nombre }}</h1>
        <button v-if="userRole === 'Profesor'" @click="showCreateModal = true" class="btn-primary">
          ➕ Nueva Tarea
        </button>
        
        <!-- Botón de diagnóstico -->
        <button @click="runFullDiagnostics" class="btn-diagnostic" :disabled="runningDiagnostics">
          {{ runningDiagnostics ? '🔍 Ejecutando...' : '🔧 Diagnóstico' }}
        </button>
      </div>

      <!-- Resultados de Diagnóstico -->
      <div v-if="diagnosticsResult" class="diagnostics-result">
        <h3>📊 Resultados del Diagnóstico</h3>
        <pre>{{ JSON.stringify(diagnosticsResult, null, 2) }}</pre>
        <button @click="diagnosticsResult = null" class="btn-outline">Cerrar</button>
      </div>

      <!-- Debug Info -->
      <div v-if="debugMode" class="debug-info">
        <h3>🔧 Debug Information</h3>
        <div class="debug-grid">
          <div><strong>User Role:</strong> {{ userRole }}</div>
          <div><strong>Course ID:</strong> {{ courseId }}</div>
          <div><strong>Assigned Courses:</strong> {{ assignedCourses.length }}</div>
          <div><strong>Has Courses:</strong> {{ hasAssignedCourses }}</div>
          <div><strong>Assignments:</strong> {{ assignments.length }}</div>
          <div><strong>Loading:</strong> {{ loading }}</div>
          <div><strong>Error:</strong> {{ error }}</div>
          <div><strong>TAREAS_API:</strong> {{ TAREAS_API }}</div>
        </div>
      </div>

      <!-- Selector de curso para profesores (solo en vista general) -->
      <div v-if="!courseId && userRole === 'Profesor'" class="course-selector">
        <div class="form-group">
          <label>Filtrar por curso:</label>
          <select v-model="selectedCourseId" @change="onCourseFilterChange">
            <option value="">Todos los cursos</option>
            <option v-for="course in assignedCourses" :key="course.id" :value="course.id">
              {{ course.nombre }} ({{ course.codigo }})
            </option>
          </select>
          <small v-if="assignedCourses.length === 0" class="warning-text">
            ⚠️ No tienes cursos asignados. Contacta al administrador.
          </small>
        </div>
      </div>

      <div v-if="loading" class="loading">
        <div class="spinner"></div>
        <p>Cargando tareas...</p>
        <p class="loading-details" v-if="loadingDetails">{{ loadingDetails }}</p>
      </div>
      
      <div v-else-if="error" class="error">
        <p>❌ {{ error }}</p>
        <div class="error-actions">
          <button @click="fetchAssignments" class="btn-outline">Reintentar</button>
          <button @click="runFullDiagnostics" class="btn-outline">Ejecutar Diagnóstico</button>
          <button @click="debugMode = !debugMode" class="btn-outline">
            {{ debugMode ? 'Ocultar Debug' : 'Mostrar Debug' }}
          </button>
        </div>
      </div>

      <div v-else>
        <!-- Estadísticas para profesores -->
        <div v-if="userRole === 'Profesor'" class="stats-grid">
          <div class="stat-card">
            <h3>Total Tareas</h3>
            <p class="stat-number">{{ assignments.length }}</p>
          </div>
          <div class="stat-card">
            <h3>Completadas</h3>
            <p class="stat-number">{{ completedAssignments }}</p>
          </div>
          <div class="stat-card">
            <h3>Pendientes</h3>
            <p class="stat-number">{{ pendingAssignments }}</p>
          </div>
        </div>

        <!-- Lista de tareas -->
        <div class="assignments-section">
          <div class="section-header">
            <h2 v-if="userRole === 'Profesor'">
              {{ courseId ? 'Tareas del Curso' : 'Mis Tareas Creadas' }}
            </h2>
            <h2 v-else>Mis Tareas</h2>
            <button @click="debugMode = !debugMode" class="debug-btn">
              {{ debugMode ? '🔧' : '🐛' }}
            </button>
          </div>
          
          <div v-if="assignments.length === 0" class="empty">
            <p v-if="userRole === 'Profesor'">
              {{ courseId ? 'No hay tareas creadas para este curso.' : 'No has creado tareas en ningún curso.' }}
            </p>
            <p v-else>No tienes tareas asignadas.</p>
            <button v-if="userRole === 'Profesor' && !courseId && hasAssignedCourses" 
                    @click="showCourseSelectModal = true" 
                    class="btn-primary">
              ➕ Crear tu primera tarea
            </button>
          </div>

          <div v-else class="assignments-grid">
            <article v-for="item in assignments" :key="item.assignment.id" class="assignment-card">
              <div class="assignment-info">
                <div class="assignment-header">
                  <h3>{{ item.assignment.title }}</h3>
                  <span class="course-badge" v-if="!courseId && item.assignment.curso_nombre">
                    {{ item.assignment.curso_nombre }}
                  </span>
                </div>
                
                <p class="description">{{ item.assignment.description }}</p>
                
                <div class="assignment-meta">
                  <span class="meta-item" v-if="item.assignment.due_date">
                    📅 {{ formatDate(item.assignment.due_date) }}
                  </span>
                  <span class="meta-item" v-if="item.assignment.points">
                    ⭐ {{ item.assignment.points }} puntos
                  </span>
                  <span class="meta-item">
                    🏷️ {{ item.assignment.assignment_type }}
                  </span>
                  <span class="meta-item" v-if="item.assignment.curso_nombre && !courseId">
                    📚 {{ item.assignment.curso_nombre }}
                  </span>
                </div>

                <!-- Estado para estudiantes -->
                <div v-if="userRole === 'Estudiante'" class="completion-status">
                  <span v-if="item.completion" class="status completed">
                    ✅ Completada - {{ formatDate(item.completion.completed_at) }}
                  </span>
                  <span v-else class="status pending">
                    ⏳ Pendiente
                  </span>
                </div>

                <!-- Estadísticas para profesores -->
                <div v-if="userRole === 'Profesor' && item.completions" class="completion-stats">
                  <span class="stats">
                    📊 {{ item.completions.completed }}/{{ item.completions.total }} estudiantes completaron
                  </span>
                </div>
              </div>

              <div class="assignment-actions">
                <button 
                  v-if="userRole === 'Estudiante' && !item.completion"
                  @click="completeAssignment(item.assignment.id)"
                  class="btn-complete"
                  :disabled="completing"
                >
                  ✅ Marcar como Completada
                </button>
                
                <button 
                  v-if="userRole === 'Profesor'"
                  @click="viewSubmissions(item.assignment.id, item.assignment.title)"
                  class="btn-primary"
                >
                  📁 Ver Entregas ({{ item.completions?.completed || 0 }})
                </button>
                
                <button 
                  v-if="userRole === 'Profesor'"
                  @click="viewAssignmentDetails(item.assignment.id)"
                  class="btn-outline"
                >
                  👀 Ver Detalles
                </button>

                <button 
                  v-if="!courseId"
                  @click="goToCourseAssignments(item.assignment.curso_id)"
                  class="btn-outline"
                >
                  📚 Ver en Curso
                </button>
              </div>
            </article>
          </div>
        </div>
      </div>

      <!-- Modal crear tarea (para curso específico) -->
      <div v-if="showCreateModal" class="modal" @click.self="showCreateModal = false">
        <div class="modal-content">
          <h2>➕ Crear Nueva Tarea</h2>
          <form @submit.prevent="createAssignment">
            <div class="form-group">
              <label>Título *</label>
              <input v-model="newAssignment.title" type="text" required>
            </div>
            
            <div class="form-group">
              <label>Descripción</label>
              <textarea v-model="newAssignment.description" rows="3"></textarea>
            </div>
            
            <div class="form-row">
              <div class="form-group">
                <label>Fecha Límite</label>
                <input v-model="newAssignment.due_date" type="datetime-local">
              </div>
              
              <div class="form-group">
                <label>Puntos</label>
                <input v-model="newAssignment.points" type="number" step="0.1" min="0">
              </div>
            </div>
            
            <div class="form-group">
              <label>Tipo de Tarea *</label>
              <select v-model="newAssignment.assignment_type" required>
                <option value="tarea">Tarea</option>
                <option value="examen">Examen</option>
                <option value="proyecto">Proyecto</option>
                <option value="quiz">Quiz</option>
                <option value="presentacion">Presentación</option>
              </select>
            </div>
            
            <div class="modal-actions">
              <button type="button" @click="showCreateModal = false" class="btn-outline">
                Cancelar
              </button>
              <button type="submit" class="btn-primary" :disabled="creating">
                {{ creating ? 'Creando...' : 'Crear Tarea' }}
              </button>
            </div>
          </form>
        </div>
      </div>

      <!-- Modal seleccionar curso para crear tarea (vista general) -->
      <div v-if="showCourseSelectModal" class="modal" @click.self="showCourseSelectModal = false">
        <div class="modal-content">
          <h2>📚 Seleccionar Curso</h2>
          <p>Selecciona un curso para crear la tarea:</p>
          
          <div v-if="assignedCourses.length === 0" class="empty-courses">
            <p>❌ No tienes cursos asignados.</p>
            <p>Contacta al administrador para que te asigne a un curso.</p>
          </div>
          
          <div v-else class="courses-list">
            <div 
              v-for="course in assignedCourses" 
              :key="course.id"
              class="course-item"
              @click="selectCourseForAssignment(course)"
            >
              <h4>{{ course.nombre }}</h4>
              <p>{{ course.codigo }}</p>
              <small>{{ course.descripcion }}</small>
            </div>
          </div>
          
          <div class="modal-actions">
            <button type="button" @click="showCourseSelectModal = false" class="btn-outline">
              Cancelar
            </button>
          </div>
        </div>
      </div>

      <!-- Modal de entregas de estudiantes -->
      <transition name="modal-fade">
        <div v-if="showSubmissionsModal" class="modal-overlay" @click="closeSubmissionsModal">
          <div class="modal-content large" @click.stop>
            <div class="modal-header">
              <h2>📁 Entregas - {{ currentAssignmentTitle }}</h2>
              <button @click="closeSubmissionsModal" class="modal-close">✕</button>
            </div>

            <div class="modal-body">
              <div v-if="loadingSubmissions" class="loading-center">
                <div class="spinner"></div>
                <p>Cargando entregas...</p>
              </div>

              <div v-else-if="submissionsError" class="error-box">
                {{ submissionsError }}
              </div>

              <div v-else-if="submissions.length === 0" class="empty-state">
                <div class="empty-icon">📭</div>
                <p>No hay entregas aún</p>
              </div>

              <div v-else class="submissions-list">
                <div v-for="submission in submissions" :key="submission.student_id" class="submission-card">
                  <div class="submission-header">
                    <div class="student-info">
                      <h3>{{ submission.student_name || `Estudiante #${submission.student_id}` }}</h3>
                      <span class="submission-date">📅 {{ formatDate(submission.completed_at) }}</span>
                      <span :class="['status-badge', submission.status]">
                        {{ getStatusText(submission.status) }}
                      </span>
                    </div>
                    <button 
                      @click="toggleFiles(submission.student_id)"
                      class="btn-toggle"
                    >
                      {{ expandedStudent === submission.student_id ? '▼' : '▶' }} 
                      Ver Archivos ({{ submission.file_count || 0 }})
                    </button>
                  </div>

                  <div v-if="submission.submitted_content" class="submission-notes">
                    <strong>Notas:</strong>
                    <p>{{ submission.submitted_content }}</p>
                  </div>

                  <!-- Lista de archivos -->
                  <transition name="expand">
                    <div v-if="expandedStudent === submission.student_id" class="files-section">
                      <div v-if="loadingFiles" class="loading-small">
                        <div class="spinner-small"></div> Cargando archivos...
                      </div>

                      <div v-else-if="studentFiles.length === 0" class="empty-files">
                        No hay archivos adjuntos
                      </div>

                      <div v-else class="files-grid">
                        <div v-for="file in studentFiles" :key="file.id" class="file-card">
                          <div class="file-icon-large">{{ getFileIcon(file.fileName) }}</div>
                          <div class="file-details">
                            <h4>{{ file.fileName }}</h4>
                            <p class="file-meta">
                              {{ formatFileSize(file.fileSize) }} • {{ formatDate(file.uploadedAt) }}
                            </p>
                            <span v-if="file.isCodeFile" class="code-badge-large">💻 Código</span>
                          </div>
                          <button 
                            @click="downloadFile(currentAssignmentId, file.id, file.fileName)"
                            class="btn-download"
                          >
                            ⬇️ Descargar
                          </button>
                        </div>
                      </div>
                    </div>
                  </transition>
                </div>
              </div>
            </div>

            <div class="modal-footer">
              <button @click="closeSubmissionsModal" class="btn-secondary">
                Cerrar
              </button>
            </div>
          </div>
        </div>
      </transition>
    </section>
  </transition>
</template>

<script setup>
import { ref, onMounted, computed, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { supabase } from '../supabase'

const route = useRoute()
const router = useRouter()

console.log('🎯 ========== INICIALIZANDO COMPONENTE ASSIGNMENTS VIEW ==========');
console.log('📌 Parámetros de ruta:', route.params);
console.log('📌 Query params:', route.query);
console.log('📌 Ruta completa:', route.path);

// Estado reactivo
const courseId = ref(route.params.id || null)
const curso = ref(null)
const assignments = ref([])
const assignedCourses = ref([])
const userRole = ref('')
const loading = ref(true)
const error = ref(null)
const creating = ref(false)
const completing = ref(false)
const showCreateModal = ref(false)
const showCourseSelectModal = ref(false)
const selectedCourseId = ref('')
const selectedCourseForAssignment = ref(null)
const debugMode = ref(false)
const loadingDetails = ref('')
const runningDiagnostics = ref(false)
const diagnosticsResult = ref(null)

// Submissions modal state
const showSubmissionsModal = ref(false)
const currentAssignmentId = ref(null)
const currentAssignmentTitle = ref('')
const submissions = ref([])
const loadingSubmissions = ref(false)
const submissionsError = ref(null)
const expandedStudent = ref(null)
const studentFiles = ref([])
const loadingFiles = ref(false)

const newAssignment = ref({
  title: '',
  description: '',
  due_date: '',
  points: null,
  assignment_type: 'tarea'
})

const TAREAS_API = import.meta.env.VITE_TAREAS_API || 'http://localhost:5011'
console.log('🌐 TAREAS_API:', TAREAS_API);

// Computed properties
const completedAssignments = computed(() => {
  return assignments.value.filter(a => a.completions?.completed > 0).length
})

const pendingAssignments = computed(() => {
  return assignments.value.length - completedAssignments.value
})

const hasAssignedCourses = computed(() => {
  console.log('🔍 Computed: hasAssignedCourses -', assignedCourses.value.length > 0, 'count:', assignedCourses.value.length);
  return assignedCourses.value.length > 0
})

// Métodos
function formatDate(dateString) {
  if (!dateString) return 'Sin fecha límite'
  return new Date(dateString).toLocaleDateString('es-ES', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}
async function runFullDiagnostics() {
  console.log('🔍 ========== EJECUTANDO DIAGNÓSTICO COMPLETO ==========');
  
  try {
    runningDiagnostics.value = true
    diagnosticsResult.value = null
    
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) {
      console.log('❌ No hay sesión para diagnóstico');
      diagnosticsResult.value = { error: 'No autenticado' }
      return
    }

    console.log('👤 Ejecutando diagnóstico para:', session.user.email);

    const response = await fetch(`${TAREAS_API}/api/debug/full-diagnostics`, {
      method: 'GET',
      headers: { 
        'Authorization': `Bearer ${session.access_token}`,
        'Content-Type': 'application/json'
      }
    })

    console.log('📡 Respuesta de diagnóstico:', response.status);
    
    if (response.ok) {
      const data = await response.json()
      console.log('✅ Diagnóstico completado:', data);
      diagnosticsResult.value = data
    } else {
      const errorText = await response.text()
      console.log('❌ Error en diagnóstico:', errorText);
      diagnosticsResult.value = { error: `HTTP ${response.status}: ${errorText}` }
    }
  } catch (e) {
    console.error('💥 Error en diagnóstico:', e);
    diagnosticsResult.value = { error: e.message }
  } finally {
    runningDiagnostics.value = false
  }
}
async function fetchAssignedCourses() {
  console.log('📚 ========== INICIANDO OBTENCIÓN DE CURSOS ASIGNADOS ==========');
  loadingDetails.value = 'Obteniendo cursos asignados...';
  
  try {
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) {
      console.log('❌ No hay sesión activa');
      loadingDetails.value = 'No hay sesión activa';
      return
    }

    console.log('👤 Sesión activa encontrada, usuario:', session.user.email);

    // Usar el endpoint del servicio de tareas
    const response = await fetch(`${TAREAS_API}/api/my-courses`, {
      method: 'GET',
      headers: { 
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${session.access_token}`
      }
    })

    console.log('📡 Respuesta de /api/my-courses:', response.status);

    if (response.ok) {
      const data = await response.json()
      console.log('✅ Cursos obtenidos:', data.cursos?.length || 0);
      assignedCourses.value = data.cursos || []
      userRole.value = data.userRole || userRole.value
      console.log('👤 Rol actualizado desde cursos:', userRole.value);
    } else {
      console.log('❌ Error obteniendo cursos, usando endpoint alternativo');
      // Intentar con el servicio de cursos
      const coursesResponse = await fetch('http://localhost:5008/api/courses/my-courses', {
        method: 'GET',
        headers: { 
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${session.access_token}`
        }
      })
      
      if (coursesResponse.ok) {
        const coursesData = await coursesResponse.json()
        assignedCourses.value = coursesData.cursos || coursesData || []
        console.log('✅ Cursos obtenidos del servicio alternativo:', assignedCourses.value.length);
      } else {
        console.log('❌ Todos los endpoints fallaron, usando datos de ejemplo');
        assignedCourses.value = [
          { id: 1, nombre: "Matemáticas Básicas", codigo: "MATH101", descripcion: "Curso de matemáticas fundamentales" },
          { id: 2, nombre: "Física I", codigo: "PHYS101", descripcion: "Introducción a la física" }
        ];
      }
    }

    console.log('📋 Cursos finales:', assignedCourses.value);
    console.log('✅ Total de cursos asignados:', assignedCourses.value.length);

  } catch (e) {
    console.error('💥 Error crítico en fetchAssignedCourses:', e);
    loadingDetails.value = 'Error crítico al obtener cursos';
    assignedCourses.value = [];
  }
}
async function testSupabaseConnection() {
  try {
    console.log('🔗 Probando conexión a Supabase...');
    const { data: { session } } = await supabase.auth.getSession();
    
    if (!session) {
      console.log('❌ No hay sesión de Supabase');
      return false;
    }
    
    console.log('✅ Sesión de Supabase activa:', session.user.email);
    
    // Probar el endpoint de salud
    const healthResponse = await fetch(`${TAREAS_API}/health`);
    console.log('🏥 Health check:', healthResponse.status);
    
    return true;
  } catch (error) {
    console.error('💥 Error en conexión:', error);
    return false;
  }
}

// Llama esta función en onMounted
onMounted(async () => {
  console.log('🚀 ========== COMPONENTE MONTADO ==========');
  console.log('📍 Ruta actual:', route.path);
  console.log('📌 Course ID:', courseId.value);
  console.log('🌐 TAREAS_API:', TAREAS_API);
  
  await testSupabaseConnection();
  await fetchAssignedCourses();
  await fetchAssignments();
  
  console.log('🏁 ========== INICIALIZACIÓN COMPLETADA ==========');
});
async function fetchAssignments() {
  console.log('📝 ========== INICIANDO CARGA DE TAREAS ==========');
  console.log('🎯 Course ID:', courseId.value);
  console.log('👤 User Role al inicio:', userRole.value);
  
  try {
    loading.value = true
    error.value = null
    loadingDetails.value = 'Iniciando carga de tareas...';
    
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) {
      error.value = 'No estás autenticado'
      console.log('❌ No hay sesión activa');
      loadingDetails.value = 'No autenticado';
      return
    }

    console.log('👤 Usuario autenticado:', session.user.email);
    console.log('🔑 Token:', session.access_token?.substring(0, 20) + '...');
    loadingDetails.value = 'Usuario autenticado, obteniendo tareas...';

    // Construir URL para obtener tareas
    let assignmentsUrl
    if (courseId.value) {
      assignmentsUrl = `${TAREAS_API}/api/courses/${courseId.value}/assignments`
    } else if (selectedCourseId.value) {
      assignmentsUrl = `${TAREAS_API}/api/courses/${selectedCourseId.value}/assignments`
    } else {
      assignmentsUrl = `${TAREAS_API}/api/assignments`
    }

    console.log('📡 URL final de tareas:', assignmentsUrl);
    loadingDetails.value = `Solicitando tareas a: ${assignmentsUrl}`;

    // Obtener tareas
    const assignmentsResponse = await fetch(assignmentsUrl, {
      method: 'GET',
      headers: { 
        'Authorization': `Bearer ${session.access_token}`,
        'Content-Type': 'application/json'
      }
    })

    console.log('📊 Respuesta HTTP de tareas:', assignmentsResponse.status, assignmentsResponse.statusText);
    loadingDetails.value = `Respuesta: ${assignmentsResponse.status} ${assignmentsResponse.statusText}`;

    if (assignmentsResponse.ok) {
      const data = await assignmentsResponse.json()
      console.log('✅ Tareas obtenidas exitosamente:', data);
      console.log('📊 Estructura completa de la respuesta:', JSON.stringify(data, null, 2));
      
      assignments.value = data.assignments || []
      
      // Usar el rol del backend si está disponible
      if (data.userRole) {
        userRole.value = data.userRole
        console.log('🔄 Rol actualizado desde backend:', userRole.value);
      }
      
      console.log('🎯 Estado final - Rol:', userRole.value, 'Tareas:', assignments.value.length);
      console.log('📋 Detalle de tareas:', assignments.value);
      loadingDetails.value = `Cargadas ${assignments.value.length} tareas`;
      
    } else {
      const errorText = await assignmentsResponse.text()
      console.log('❌ Error en respuesta de tareas:', errorText);
      error.value = `Error al cargar tareas: ${assignmentsResponse.status} ${assignmentsResponse.statusText}`
      loadingDetails.value = `Error: ${assignmentsResponse.status}`;
    }
  } catch (e) {
    console.error('💥 Error crítico en fetchAssignments:', e);
    error.value = `Error de conexión: ${e.message}`
    loadingDetails.value = `Error crítico: ${e.message}`;
  } finally {
    loading.value = false
    console.log('🏁 ========== CARGA DE TAREAS COMPLETADA ==========');
  }
}

async function createAssignment() {
  console.log('➕ ========== INICIANDO CREACIÓN DE TAREA ==========');
  
  try {
    creating.value = true
    
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) {
      alert('No estás autenticado');
      return
    }

    const targetCourseId = courseId.value || selectedCourseForAssignment.value?.id
    if (!targetCourseId) {
      alert('No se ha seleccionado un curso válido');
      return
    }

    // 🔹 VALIDACIONES MEJORADAS
    if (!newAssignment.value.title?.trim()) {
      alert('El título es obligatorio');
      return;
    }

    const assignmentData = {
      title: newAssignment.value.title.trim(),
      description: newAssignment.value.description?.trim() || '',
      dueDate: newAssignment.value.due_date ? new Date(newAssignment.value.due_date).toISOString() : null,
      points: newAssignment.value.points ? parseFloat(newAssignment.value.points) : null,
      assignmentType: newAssignment.value.assignment_type || 'tarea',
      cursoId: parseInt(targetCourseId),
      createdBy: session.user.email
    }

    console.log('📤 Enviando datos:', assignmentData);

    const response = await fetch(`${TAREAS_API}/api/courses/${targetCourseId}/assignments`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${session.access_token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(assignmentData)
    })

    console.log('📡 Respuesta del servidor:', response.status, response.statusText);

    if (response.ok) {
      const data = await response.json()
      console.log('✅ Tarea creada exitosamente:', data);
      
      await fetchAssignments()
      showCreateModal.value = false
      showCourseSelectModal.value = false
      resetNewAssignment()
      alert('✅ Tarea creada correctamente')
    } else {
      let errorMessage = 'Error al crear tarea';
      try {
        const errorData = await response.json();
        errorMessage = errorData.detail || errorMessage;
        console.log('❌ Error del servidor:', errorData);
      } catch (e) {
        const errorText = await response.text();
        console.log('❌ Error texto:', errorText);
        errorMessage = `${response.status} ${response.statusText}: ${errorText}`;
      }
      alert('❌ ' + errorMessage);
    }
  } catch (e) {
    console.error('💥 Error en createAssignment:', e);
    alert('Error de conexión: ' + e.message);
  } finally {
    creating.value = false;
  }
}

async function completeAssignment(assignmentId) {
  console.log('✅ ========== COMPLETANDO TAREA ==========');
  console.log('🎯 Assignment ID:', assignmentId);
  
  try {
    completing.value = true
    
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) {
      console.log('❌ No hay sesión para completar tarea');
      return
    }

    const response = await fetch(`${TAREAS_API}/api/assignments/${assignmentId}/complete`, {
      method: 'POST',
      headers: { 
        'Authorization': `Bearer ${session.access_token}`,
        'Content-Type': 'application/json'
      }
    })

    console.log('📡 Respuesta de completado:', response.status);

    if (response.ok) {
      console.log('✅ Tarea marcada como completada');
      
      // Actualizar estado local
      const assignment = assignments.value.find(a => a.assignment.id === assignmentId)
      if (assignment) {
        assignment.completion = {
          completed_at: new Date().toISOString(),
          status: 'completed'
        }
      }
      
      alert('✅ Tarea marcada como completada');
    } else {
      const errorData = await response.json().catch(() => ({ detail: 'Error desconocido' }))
      console.log('❌ Error completando tarea:', errorData);
      alert(errorData.detail || 'Error al completar tarea')
    }
  } catch (e) {
    console.error('💥 Error en completeAssignment:', e)
    alert('Error al completar tarea: ' + e.message)
  } finally {
    completing.value = false
  }
}

// En AssignmentsView.vue, modifica la función:
function viewAssignmentDetails(assignmentId) {
  console.log('👀 Viendo detalles de tarea:', assignmentId);
  
  // En lugar de navegar, muestra un modal o alert simple
  const assignment = assignments.value.find(a => a.assignment.id === assignmentId);
  if (assignment) {
    alert(`📝 ${assignment.assignment.title}\n\n📚 ${assignment.assignment.curso_nombre}\n📅 ${formatDate(assignment.assignment.due_date)}\n⭐ ${assignment.assignment.points} puntos\n🏷️ ${assignment.assignment.assignment_type}`);
  }
}

function goToCourseAssignments(cursoId) {
  console.log('📚 Navegando a tareas del curso:', cursoId);
  router.push(`/courses/${cursoId}/assignments`)
}

function selectCourseForAssignment(course) {
  console.log('🎯 Curso seleccionado para tarea:', course);
  selectedCourseForAssignment.value = course
  showCourseSelectModal.value = false
  showCreateModal.value = true
}

// Submissions modal functions
async function viewSubmissions(assignmentId, assignmentTitle) {
  currentAssignmentId.value = assignmentId
  currentAssignmentTitle.value = assignmentTitle
  showSubmissionsModal.value = true
  expandedStudent.value = null
  await loadSubmissions(assignmentId)
}

async function loadSubmissions(assignmentId) {
  loadingSubmissions.value = true
  submissionsError.value = null
  
  try {
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) return

    const response = await fetch(
      `${TAREAS_API}/api/assignments/${assignmentId}/submissions`,
      {
        headers: {
          'Authorization': `Bearer ${session.access_token}`,
          'X-User-Email': session.user.email
        }
      }
    )

    if (response.ok) {
      const data = await response.json()
      submissions.value = data.submissions || []
    } else {
      submissionsError.value = 'Error al cargar las entregas'
    }
  } catch (e) {
    console.error('Error loading submissions:', e)
    submissionsError.value = 'Error de conexión'
  } finally {
    loadingSubmissions.value = false
  }
}

async function toggleFiles(studentId) {
  if (expandedStudent.value === studentId) {
    expandedStudent.value = null
    studentFiles.value = []
    return
  }

  expandedStudent.value = studentId
  loadingFiles.value = true
  
  try {
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) return

    const response = await fetch(
      `${TAREAS_API}/api/assignments/${currentAssignmentId.value}/files?studentId=${studentId}`,
      {
        headers: {
          'Authorization': `Bearer ${session.access_token}`,
          'X-User-Email': session.user.email
        }
      }
    )

    if (response.ok) {
      const data = await response.json()
      studentFiles.value = data.files || []
    }
  } catch (e) {
    console.error('Error loading files:', e)
  } finally {
    loadingFiles.value = false
  }
}

async function downloadFile(assignmentId, fileId, fileName) {
  try {
    const { data: { session } } = await supabase.auth.getSession()
    if (!session) return

    const response = await fetch(
      `${TAREAS_API}/api/assignments/${assignmentId}/files/${fileId}`,
      {
        headers: {
          'Authorization': `Bearer ${session.access_token}`,
          'X-User-Email': session.user.email
        }
      }
    )

    if (response.ok) {
      const blob = await response.blob()
      const url = window.URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = fileName
      document.body.appendChild(a)
      a.click()
      window.URL.revokeObjectURL(url)
      document.body.removeChild(a)
    } else {
      alert('Error al descargar archivo')
    }
  } catch (e) {
    console.error('Error downloading file:', e)
    alert('Error al descargar archivo')
  }
}

function closeSubmissionsModal() {
  showSubmissionsModal.value = false
  currentAssignmentId.value = null
  currentAssignmentTitle.value = ''
  submissions.value = []
  studentFiles.value = []
  expandedStudent.value = null
}

function getStatusText(status) {
  const statusMap = {
    'completed': 'Completada',
    'completed_with_errors': 'Completada con errores',
    'pending': 'Pendiente'
  }
  return statusMap[status] || status
}

function getFileIcon(filename) {
  const ext = filename.substring(filename.lastIndexOf('.')).toLowerCase()
  if (['.py', '.js', '.ts', '.jsx', '.tsx', '.java', '.cpp', '.c', '.cs'].includes(ext)) return '💻'
  if (['.pdf'].includes(ext)) return '📄'
  if (['.doc', '.docx'].includes(ext)) return '📝'
  if (['.zip', '.rar'].includes(ext)) return '📦'
  if (['.png', '.jpg', '.jpeg', '.gif'].includes(ext)) return '🖼️'
  if (['.html', '.css'].includes(ext)) return '🌐'
  return '📎'
}

function formatFileSize(bytes) {
  if (bytes === 0) return '0 Bytes'
  const k = 1024
  const sizes = ['Bytes', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i]
}

function onCourseFilterChange() {
  console.log('🔍 Cambiando filtro de curso:', selectedCourseId.value);
  fetchAssignments()
}

function resetNewAssignment() {
  newAssignment.value = {
    title: '',
    description: '',
    due_date: '',
    points: null,
    assignment_type: 'tarea'
  }
}

// Watchers
watch(() => route.params.id, (newId) => {
  console.log('🔄 Cambio en parámetro de ruta:', newId);
  courseId.value = newId
  fetchAssignments()
})

// Lifecycle
onMounted(async () => {
  console.log('🚀 ========== COMPONENTE MONTADO ==========');
  console.log('📍 Ruta actual:', route.path);
  console.log('📌 Course ID:', courseId.value);
  console.log('🌐 TAREAS_API:', TAREAS_API);
  console.log('🔧 Debug Mode:', debugMode.value);
  
  await fetchAssignedCourses()
  await fetchAssignments()
  
  console.log('🏁 ========== INICIALIZACIÓN COMPLETADA ==========');
})
</script>

<style scoped>
.btn-diagnostic {
  background: #6366f1;
  color: #fff;
  border: none;
  padding: .5rem 1rem;
  border-radius: 8px;
  font-weight: 600;
  cursor: pointer;
  margin-left: 0.5rem;
  transition: background .2s;
}

.btn-diagnostic:hover {
  background: #4f46e5;
}

.btn-diagnostic:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.diagnostics-result {
  background: #f8fafc;
  border: 2px solid #e2e8f0;
  border-radius: 12px;
  padding: 1.5rem;
  margin-bottom: 1.5rem;
  max-height: 400px;
  overflow-y: auto;
}

.diagnostics-result h3 {
  margin: 0 0 1rem 0;
  color: #374151;
}

.diagnostics-result pre {
  background: #1f2937;
  color: #f9fafb;
  padding: 1rem;
  border-radius: 8px;
  font-size: 0.8rem;
  overflow-x: auto;
  margin-bottom: 1rem;
}
.wrap { max-width: 1100px; margin: 2rem auto; padding: 0 1rem; animation: slideIn .7s; }
.header { display: flex; align-items: center; gap: 1rem; margin-bottom: 1.5rem; }
.btn-back { background: #fff; color: #2a4dd0; border: 2px solid #2a4dd0; padding: .5rem 1rem; border-radius: 8px; font-weight: 600; cursor: pointer; }
.btn-primary { background: #10b981; color: #fff; border: none; padding: .5rem 1rem; border-radius: 8px; font-weight: 600; cursor: pointer; margin-left: auto; transition: background .2s; }
.btn-primary:hover { background: #059669; }
.btn-primary:disabled { opacity: 0.6; cursor: not-allowed; background: #6b7280; }

/* Debug Info */
.debug-info { background: #f3f4f6; border: 1px solid #d1d5db; border-radius: 8px; padding: 1rem; margin-bottom: 1rem; }
.debug-info h3 { margin: 0 0 0.5rem 0; color: #374151; }
.debug-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 0.5rem; }
.debug-grid div { padding: 0.25rem; background: white; border-radius: 4px; }

/* Course Selector */
.course-selector { margin-bottom: 1.5rem; padding: 1rem; background: #f8fafc; border-radius: 8px; }
.course-selector .form-group { margin-bottom: 0; }
.course-selector label { font-weight: 600; margin-bottom: 0.5rem; display: block; }
.course-selector select { width: 100%; max-width: 300px; }
.warning-text { color: #dc2626; font-weight: 500; }

/* Loading */
.loading { text-align: center; padding: 2rem; }
.spinner { border: 3px solid #f3f3f3; border-top: 3px solid #2a4dd0; border-radius: 50%; width: 40px; height: 40px; animation: spin 1s linear infinite; margin: 0 auto 1rem; }
.loading-details { color: #6b7280; font-size: 0.9rem; margin-top: 0.5rem; }
@keyframes spin { 0% { transform: rotate(0deg); } 100% { transform: rotate(360deg); } }

/* Error */
.error { text-align: center; padding: 2rem; color: #ef4444; }
.error-actions { display: flex; gap: 0.5rem; justify-content: center; margin-top: 1rem; flex-wrap: wrap; }

/* Empty States */
.empty { text-align: center; padding: 2rem; color: #666; }
.empty-courses { text-align: center; padding: 2rem; color: #dc2626; }

/* Stats */
.stats-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 1rem; margin-bottom: 2rem; }
.stat-card { background: #fff; border-radius: 12px; padding: 1.5rem; text-align: center; box-shadow: 0 2px 8px rgba(0,0,0,.06); }
.stat-number { font-size: 2rem; font-weight: bold; color: #2a4dd0; margin: .5rem 0 0; }

/* Assignments */
.assignments-section h2 { margin-bottom: 1rem; color: #2a4dd0; }
.assignment-card { background: #fff; border-radius: 12px; padding: 1.5rem; box-shadow: 0 2px 8px rgba(0,0,0,.06); display: flex; justify-content: space-between; align-items: flex-start; transition: transform .18s ease, box-shadow .2s ease; }
.assignment-card:hover { transform: translateY(-2px); box-shadow: 0 4px 12px rgba(0,0,0,.08); }
.assignment-info { flex: 1; }
.assignment-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 0.5rem; }
.course-badge { background: #e0f2fe; color: #075985; padding: 0.25rem 0.5rem; border-radius: 6px; font-size: 0.8rem; font-weight: 600; }
.assignment-info h3 { margin: 0 0 .5rem; color: #222; font-size: 1.2rem; }
.description { margin: .5rem 0; color: #555; line-height: 1.4; }
.assignment-meta { display: flex; gap: 1rem; margin: .75rem 0; flex-wrap: wrap; }
.meta-item { background: #f8fafc; padding: .25rem .5rem; border-radius: 6px; font-size: .85rem; color: #64748b; }

/* Status */
.completion-status, .completion-stats { margin-top: .75rem; }
.status { padding: .25rem .75rem; border-radius: 20px; font-size: .9rem; font-weight: 600; }
.status.completed { background: #d1fae5; color: #065f46; }
.status.pending { background: #fef3c7; color: #92400e; }
.stats { background: #e0f2fe; color: #075985; padding: .25rem .75rem; border-radius: 20px; font-size: .9rem; }

/* Actions */
.assignment-actions { margin-left: 1rem; display: flex; flex-direction: column; gap: 0.5rem; min-width: 150px; }
.btn-complete { background: #10b981; color: #fff; border: none; padding: .5rem 1rem; border-radius: 8px; font-weight: 600; cursor: pointer; font-size: .9rem; transition: background .2s; }
.btn-complete:hover { background: #059669; }
.btn-complete:disabled { opacity: .5; cursor: not-allowed; }
.btn-outline { background: #fff; color: #2a4dd0; border: 2px solid #2a4dd0; padding: .5rem 1rem; border-radius: 8px; font-weight: 600; cursor: pointer; font-size: .9rem; text-align: center; }

/* Modal */
.modal { position: fixed; inset: 0; background: rgba(0,0,0,.5); display: grid; place-items: center; z-index: 100; animation: fadeIn .3s; }
.modal-content { background: #fff; border-radius: 16px; padding: 2rem; max-width: 500px; width: 90%; max-height: 90vh; overflow-y: auto; }
.modal-content h2 { margin-bottom: 1.5rem; color: #2a4dd0; }

/* Courses List */
.courses-list { margin: 1rem 0; max-height: 300px; overflow-y: auto; }
.course-item { padding: 1rem; border: 1px solid #e5e7eb; border-radius: 8px; margin-bottom: 0.5rem; cursor: pointer; transition: background-color 0.2s; }
.course-item:hover { background-color: #f8fafc; }
.course-item h4 { margin: 0 0 0.5rem; color: #2a4dd0; }
.course-item p { margin: 0; font-weight: 600; color: #374151; }
.course-item small { color: #6b7280; }

/* Form */
.form-group { margin-bottom: 1rem; }
.form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
.form-group label { display: block; margin-bottom: .5rem; font-weight: 600; color: #374151; }
.form-group input, .form-group textarea, .form-group select { width: 100%; padding: .75rem; border: 2px solid #e5e7eb; border-radius: 8px; font-size: 1rem; transition: border-color .2s; }
.form-group input:focus, .form-group textarea:focus, .form-group select:focus { outline: none; border-color: #2a4dd0; }
.modal-actions { display: flex; gap: .5rem; justify-content: flex-end; margin-top: 1.5rem; }

/* Animations */
.fade-enter-active, .fade-leave-active { transition: opacity .4s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
@keyframes slideIn { from { transform: translateY(20px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }
@keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }

/* Submissions Modal */
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

.modal-content.large {
  max-width: 900px;
  width: 100%;
  max-height: 90vh;
  overflow-y: auto;
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
  border-radius: 4px;
  transition: background 0.2s;
}

.modal-close:hover {
  background: #f3f4f6;
}

.modal-body {
  padding: 1.5rem;
}

.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: 0.75rem;
  padding: 1.5rem;
  border-top: 1px solid #e5e7eb;
}

.modal-fade-enter-active, .modal-fade-leave-active {
  transition: opacity 0.3s;
}

.modal-fade-enter-from, .modal-fade-leave-to {
  opacity: 0;
}

.loading-center {
  text-align: center;
  padding: 3rem;
}

.empty-state {
  text-align: center;
  padding: 3rem;
  color: #6b7280;
}

.empty-icon {
  font-size: 4rem;
  margin-bottom: 1rem;
}

.submissions-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.submission-card {
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  padding: 1.5rem;
}

.submission-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.student-info {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.student-info h3 {
  margin: 0;
  color: #1f2937;
  font-size: 1.125rem;
}

.submission-date {
  font-size: 0.875rem;
  color: #6b7280;
}

.status-badge {
  display: inline-block;
  padding: 0.25rem 0.75rem;
  border-radius: 12px;
  font-size: 0.75rem;
  font-weight: 600;
  text-transform: uppercase;
}

.status-badge.completed {
  background: #d1fae5;
  color: #065f46;
}

.status-badge.completed_with_errors {
  background: #fef3c7;
  color: #92400e;
}

.status-badge.pending {
  background: #e0e7ff;
  color: #3730a3;
}

.btn-toggle {
  background: #f3f4f6;
  border: none;
  padding: 0.5rem 1rem;
  border-radius: 8px;
  cursor: pointer;
  font-weight: 600;
  transition: background 0.2s;
}

.btn-toggle:hover {
  background: #e5e7eb;
}

.submission-notes {
  background: #f9fafb;
  padding: 1rem;
  border-radius: 8px;
  margin-bottom: 1rem;
}

.submission-notes strong {
  color: #374151;
}

.submission-notes p {
  margin: 0.5rem 0 0 0;
  color: #6b7280;
}

.files-section {
  margin-top: 1rem;
  padding-top: 1rem;
  border-top: 1px solid #e5e7eb;
}

.expand-enter-active, .expand-leave-active {
  transition: all 0.3s ease;
  max-height: 1000px;
  overflow: hidden;
}

.expand-enter-from, .expand-leave-to {
  max-height: 0;
  opacity: 0;
}

.loading-small {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: #6b7280;
  padding: 1rem;
}

.spinner-small {
  width: 16px;
  height: 16px;
  border: 2px solid #f3f4f6;
  border-top-color: #2a4dd0;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

.empty-files {
  text-align: center;
  padding: 2rem;
  color: #9ca3af;
}

.files-grid {
  display: grid;
  gap: 1rem;
}

.file-card {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1rem;
  background: #f9fafb;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  transition: background 0.2s;
}

.file-card:hover {
  background: #f3f4f6;
}

.file-icon-large {
  font-size: 2rem;
  min-width: 50px;
  text-align: center;
}

.file-details {
  flex: 1;
}

.file-details h4 {
  margin: 0 0 0.25rem 0;
  color: #1f2937;
  font-size: 0.875rem;
}

.file-meta {
  margin: 0;
  font-size: 0.75rem;
  color: #6b7280;
}

.code-badge-large {
  display: inline-block;
  background: #dbeafe;
  color: #1e40af;
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  font-size: 0.75rem;
  font-weight: 600;
  margin-top: 0.25rem;
}

.btn-download {
  background: #2a4dd0;
  color: white;
  border: none;
  padding: 0.5rem 1rem;
  border-radius: 8px;
  cursor: pointer;
  font-weight: 600;
  transition: all 0.2s;
  white-space: nowrap;
}

.btn-download:hover {
  background: #1e3a8a;
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(42, 77, 208, 0.3);
}

@media (max-width: 768px) {
  .header { flex-wrap: wrap; }
  .btn-primary { margin-left: 0; width: 100%; }
  .assignment-card { flex-direction: column; }
  .assignment-actions { margin-left: 0; margin-top: 1rem; width: 100%; }
  .assignment-actions button { width: 100%; }
  .form-row { grid-template-columns: 1fr; }
  .assignment-header { flex-direction: column; gap: 0.5rem; }
  
  .submission-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 1rem;
  }
  
  .btn-toggle {
    width: 100%;
  }
  
  .file-card {
    flex-direction: column;
    text-align: center;
  }
  
  .btn-download {
    width: 100%;
  }
}
</style>