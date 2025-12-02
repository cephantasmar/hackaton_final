<template>
  <div class="plagiarism-dashboard">
    <!-- Header -->
    <div class="dashboard-header">
      <div class="header-content">
        <div class="header-left">
          <h1>
            <i class="fas fa-search"></i>
            Sistema de Detección de Plagio
          </h1>
          <p class="subtitle">
            <i class="fas fa-user-tie"></i>
            Panel del Profesor - {{ tenantDisplayName }}
          </p>
        </div>
        <div class="header-right">
          <div class="user-info">
            <div class="avatar">
              <i class="fas fa-user-graduate"></i>
            </div>
            <div class="user-details">
              <span class="user-name">{{ currentUser?.nombre || 'Profesor' }}</span>
              <span class="user-email">{{ currentUser?.email || userEmail }}</span>
              <span class="user-role">Profesor</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Panel de Estadísticas -->
    <div class="stats-panel">
      <div class="stat-card" @click="filterByStatus('all')">
        <div class="stat-icon total">
          <i class="fas fa-file-alt"></i>
        </div>
        <div class="stat-info">
          <h3>{{ totalAssignments }}</h3>
          <p>Total Entregas</p>
        </div>
      </div>
      
      <div class="stat-card" @click="filterByStatus('pending')">
        <div class="stat-icon pending">
          <i class="fas fa-clock"></i>
        </div>
        <div class="stat-info">
          <h3>{{ pendingAnalysis }}</h3>
          <p>Pendientes de Análisis</p>
        </div>
      </div>
      
      <div class="stat-card" @click="filterByStatus('suspicious')">
        <div class="stat-icon suspicious">
          <i class="fas fa-exclamation-triangle"></i>
        </div>
        <div class="stat-info">
          <h3>{{ suspiciousAssignments }}</h3>
          <p>Plagio Sospechoso</p>
        </div>
      </div>
      
      <div class="stat-card" @click="filterByStatus('clean')">
        <div class="stat-icon clean">
          <i class="fas fa-check-circle"></i>
        </div>
        <div class="stat-info">
          <h3>{{ cleanAssignments }}</h3>
          <p>Entregas Limpias</p>
        </div>
      </div>
    </div>

    <!-- Panel de Controles -->
    <div class="control-panel">
      <div class="search-section">
        <div class="search-box">
          <i class="fas fa-search"></i>
          <input 
            v-model="searchQuery" 
            @input="filterAssignments" 
            placeholder="Buscar por estudiante, archivo o trabajo..."
            class="search-input"
          />
        </div>
        
        <div class="filter-group">
          <select v-model="selectedAssignment" @change="filterAssignments" class="filter-select">
            <option value="">Todos los trabajos</option>
            <option v-for="assignment in uniqueAssignments" :key="assignment" :value="assignment">
              {{ assignment }}
            </option>
          </select>
          
          <select v-model="selectedStatus" @change="filterAssignments" class="filter-select">
            <option value="">Todos los estados</option>
            <option value="pending">Pendiente</option>
            <option value="analyzing">Analizando</option>
            <option value="completed">Completado</option>
            <option value="error">Error</option>
          </select>
          
          <select v-model="similarityThreshold" @change="filterAssignments" class="filter-select">
            <option value="0">Todas las similitudes</option>
            <option value="70">Alta similitud (≥70%)</option>
            <option value="40">Media similitud (≥40%)</option>
            <option value="20">Baja similitud (≥20%)</option>
            <option value="0">Sin similitud</option>
          </select>
        </div>
      </div>
      
      <div class="action-buttons">
        <button @click="refreshAssignments" class="btn btn-refresh" :disabled="loading">
          <i class="fas fa-sync-alt" :class="{ 'fa-spin': loading }"></i>
          Actualizar
        </button>
        
        <button @click="analyzeAllPending" class="btn btn-analyze-all" :disabled="loading || !hasPendingAssignments">
          <i class="fas fa-search"></i>
          Analizar Pendientes ({{ pendingAnalysis }})
        </button>
        
        <button @click="exportToCSV" class="btn btn-export">
          <i class="fas fa-download"></i>
          Exportar CSV
        </button>
      </div>
    </div>

    <!-- Tabla de Entregas -->
    <div class="assignments-table">
      <div class="table-header">
        <h3>Entregas de Estudiantes ({{ filteredAssignments.length }})</h3>
        <div class="table-actions">
          <span v-if="selectedStatus" class="filter-tag">
            {{ getStatusDisplay(selectedStatus) }}
            <button @click="selectedStatus = ''; filterAssignments()" class="tag-remove">
              <i class="fas fa-times"></i>
            </button>
          </span>
          <span v-if="selectedAssignment" class="filter-tag">
            {{ selectedAssignment }}
            <button @click="selectedAssignment = ''; filterAssignments()" class="tag-remove">
              <i class="fas fa-times"></i>
            </button>
          </span>
        </div>
      </div>
      
      <div v-if="loading" class="loading-container">
        <div class="spinner"></div>
        <p>Cargando entregas...</p>
      </div>
      
      <div v-else-if="filteredAssignments.length === 0" class="empty-state">
        <i class="fas fa-file-alt"></i>
        <h4>No se encontraron entregas</h4>
        <p v-if="searchQuery || selectedAssignment || selectedStatus">
          Intenta modificar los filtros de búsqueda
        </p>
        <p v-else>No hay entregas disponibles para mostrar</p>
      </div>
      
      <div v-else class="table-responsive">
        <table class="assignments-list">
          <thead>
            <tr>
              <th @click="sortBy('student_name')" class="sortable">
                Estudiante
                <i v-if="sortField === 'student_name'" class="fas" :class="sortDirection === 'asc' ? 'fa-sort-up' : 'fa-sort-down'"></i>
                <i v-else class="fas fa-sort"></i>
              </th>
              <th @click="sortBy('assignment_name')" class="sortable">
                Trabajo
                <i v-if="sortField === 'assignment_name'" class="fas" :class="sortDirection === 'asc' ? 'fa-sort-up' : 'fa-sort-down'"></i>
                <i v-else class="fas fa-sort"></i>
              </th>
              <th @click="sortBy('file_name')" class="sortable">
                Archivo
                <i v-if="sortField === 'file_name'" class="fas" :class="sortDirection === 'asc' ? 'fa-sort-up' : 'fa-sort-down'"></i>
                <i v-else class="fas fa-sort"></i>
              </th>
              <th @click="sortBy('uploaded_at')" class="sortable">
                Fecha
                <i v-if="sortField === 'uploaded_at'" class="fas" :class="sortDirection === 'asc' ? 'fa-sort-up' : 'fa-sort-down'"></i>
                <i v-else class="fas fa-sort"></i>
              </th>
              <th @click="sortBy('similarity_score')" class="sortable">
                Similitud
                <i v-if="sortField === 'similarity_score'" class="fas" :class="sortDirection === 'asc' ? 'fa-sort-up' : 'fa-sort-down'"></i>
                <i v-else class="fas fa-sort"></i>
              </th>
              <th>Estado</th>
              <th>Acciones</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="assignment in filteredAssignments" :key="assignment.id" 
                :class="getRowClass(assignment)">
              <td class="student-cell">
                <div class="student-info">
                  <div class="student-avatar">
                    <i class="fas fa-user-graduate"></i>
                  </div>
                  <div class="student-details">
                    <strong>{{ assignment.student_name }}</strong>
                    <small>{{ assignment.student_email }}</small>
                  </div>
                </div>
              </td>
              
              <td class="assignment-cell">
                <div class="assignment-info">
                  <strong>{{ assignment.assignment_name }}</strong>
                  <small v-if="assignment.course_name">{{ assignment.course_name }}</small>
                </div>
              </td>
              
              <td class="file-cell">
                <div class="file-info">
                  <i :class="getFileIcon(assignment.file_type)"></i>
                  <div>
                    <strong>{{ assignment.file_name }}</strong>
                    <small>{{ formatFileSize(assignment.file_size) }}</small>
                  </div>
                </div>
              </td>
              
              <td class="date-cell">
                {{ formatDate(assignment.uploaded_at) }}
              </td>
              
              <td class="similarity-cell">
                <div v-if="assignment.analysis_status === 'completed'" class="similarity-display">
                  <div class="similarity-score" :class="getSimilarityClass(assignment.similarity_score)">
                    {{ assignment.similarity_score }}%
                  </div>
                  <div class="similarity-bar">
                    <div class="similarity-fill" 
                         :style="{ width: assignment.similarity_score + '%' }"
                         :class="getSimilarityClass(assignment.similarity_score)"></div>
                  </div>
                </div>
                <div v-else class="similarity-placeholder">
                  --
                </div>
              </td>
              
              <td class="status-cell">
                <span class="status-badge" :class="getStatusClass(assignment.analysis_status)">
                  {{ getStatusDisplay(assignment.analysis_status) }}
                </span>
                <small v-if="assignment.analyzed_at" class="status-time">
                  {{ formatRelativeTime(assignment.analyzed_at) }}
                </small>
              </td>
              
              <td class="actions-cell">
                <button v-if="assignment.analysis_status === 'pending'" 
                        @click="analyzeAssignment(assignment)"
                        class="btn-action analyze-btn"
                        :disabled="assignment.analyzing">
                  <i v-if="assignment.analyzing" class="fas fa-spinner fa-spin"></i>
                  <i v-else class="fas fa-search"></i>
                  Analizar
                </button>
                
                <button v-else-if="assignment.analysis_status === 'completed'" 
                        @click="viewDetails(assignment)"
                        class="btn-action details-btn">
                  <i class="fas fa-chart-bar"></i>
                  Detalles
                </button>
                
                <button v-else-if="assignment.analysis_status === 'error'" 
                        @click="retryAnalysis(assignment)"
                        class="btn-action retry-btn">
                  <i class="fas fa-redo"></i>
                  Reintentar
                </button>
                
                <button @click="downloadFile(assignment)" 
                        class="btn-action download-btn"
                        title="Descargar archivo">
                  <i class="fas fa-download"></i>
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Paginación -->
    <div v-if="filteredAssignments.length > 0" class="pagination">
      <button @click="prevPage" :disabled="currentPage === 1" class="page-btn">
        <i class="fas fa-chevron-left"></i>
      </button>
      
      <span class="page-info">
        Página {{ currentPage }} de {{ totalPages }}
      </span>
      
      <button @click="nextPage" :disabled="currentPage === totalPages" class="page-btn">
        <i class="fas fa-chevron-right"></i>
      </button>
    </div>

    <!-- Modal de Detalles -->
    <div v-if="selectedAssignmentDetails" class="modal-overlay" @click.self="closeModal">
      <div class="modal-content">
        <div class="modal-header">
          <h3>
            <i class="fas fa-chart-bar"></i>
            Resultados de Análisis de Plagio
          </h3>
          <button @click="closeModal" class="modal-close">
            <i class="fas fa-times"></i>
          </button>
        </div>
        
        <div class="modal-body">
          <div class="detail-section">
            <h4>Información del Archivo</h4>
            <div class="file-details-grid">
              <div class="detail-item">
                <label>Estudiante:</label>
                <span>{{ selectedAssignmentDetails.student_name }}</span>
              </div>
              <div class="detail-item">
                <label>Trabajo:</label>
                <span>{{ selectedAssignmentDetails.assignment_name }}</span>
              </div>
              <div class="detail-item">
                <label>Archivo:</label>
                <span>{{ selectedAssignmentDetails.file_name }}</span>
              </div>
              <div class="detail-item">
                <label>Tamaño:</label>
                <span>{{ formatFileSize(selectedAssignmentDetails.file_size) }}</span>
              </div>
              <div class="detail-item">
                <label>Fecha de subida:</label>
                <span>{{ formatDate(selectedAssignmentDetails.uploaded_at) }}</span>
              </div>
              <div class="detail-item">
                <label>Fecha de análisis:</label>
                <span>{{ formatDate(selectedAssignmentDetails.analyzed_at) }}</span>
              </div>
            </div>
          </div>
          
          <div class="detail-section">
            <h4>Resultados de Similitud</h4>
            <div class="similarity-summary">
              <div class="similarity-main">
                <div class="similarity-score-large" :class="getSimilarityClass(selectedAssignmentDetails.similarity_score)">
                  {{ selectedAssignmentDetails.similarity_score }}%
                </div>
                <div class="similarity-label">
                  Nivel de similitud total detectado
                </div>
              </div>
              
              <div class="similarity-breakdown">
                <div class="breakdown-item high">
                  <span class="breakdown-label">Alta similitud (≥70%):</span>
                  <span class="breakdown-value">{{ selectedAssignmentDetails.high_similarity_sources || 0 }} fuentes</span>
                </div>
                <div class="breakdown-item medium">
                  <span class="breakdown-label">Media similitud (40-69%):</span>
                  <span class="breakdown-value">{{ selectedAssignmentDetails.medium_similarity_sources || 0 }} fuentes</span>
                </div>
                <div class="breakdown-item low">
                  <span class="breakdown-label">Baja similitud (≤39%):</span>
                  <span class="breakdown-value">{{ selectedAssignmentDetails.low_similarity_sources || 0 }} fuentes</span>
                </div>
              </div>
            </div>
          </div>
          
          <div v-if="selectedAssignmentDetails.similar_sources && selectedAssignmentDetails.similar_sources.length > 0" 
               class="detail-section">
            <h4>Fuentes Similares Detectadas</h4>
            <div class="sources-list">
              <div v-for="(source, index) in selectedAssignmentDetails.similar_sources" 
                   :key="index" 
                   class="source-item">
                <div class="source-header">
                  <div class="source-info">
                    <strong>{{ source.source_name }}</strong>
                    <small>{{ source.source_type }}</small>
                  </div>
                  <span class="source-similarity" :class="getSimilarityClass(source.similarity_percentage)">
                    {{ source.similarity_percentage }}%
                  </span>
                </div>
                <div class="source-details">
                  <p v-if="source.url">
                    <i class="fas fa-link"></i>
                    <a :href="source.url" target="_blank">{{ source.url }}</a>
                  </p>
                  <p v-if="source.matched_lines">
                    <i class="fas fa-code"></i>
                    Líneas coincidentes: {{ source.matched_lines }}
                  </p>
                  <p v-if="source.detected_at">
                    <i class="fas fa-calendar-alt"></i>
                    Detectado: {{ formatDate(source.detected_at) }}
                  </p>
                </div>
              </div>
            </div>
          </div>
          
          <div v-else class="detail-section">
            <div class="no-sources">
              <i class="fas fa-check-circle"></i>
              <p>No se encontraron fuentes con similitud significativa</p>
            </div>
          </div>
          
          <div class="detail-section">
            <h4>Información Técnica</h4>
            <div class="tech-info">
              <p>
                <i class="fas fa-cogs"></i>
                Análisis realizado usando Codequiry API
              </p>
              <p>
                <i class="fas fa-database"></i>
                Base de datos: {{ selectedAssignmentDetails.database_size || '20M+' }} fuentes
              </p>
              <p>
                <i class="fas fa-clock"></i>
                Tiempo de análisis: {{ selectedAssignmentDetails.analysis_time || 'N/A' }}
              </p>
            </div>
          </div>
        </div>
        
        <div class="modal-footer">
          <button @click="downloadReport(selectedAssignmentDetails)" class="btn btn-export">
            <i class="fas fa-file-pdf"></i>
            Descargar Reporte PDF
          </button>
          <button @click="closeModal" class="btn btn-close">
            Cerrar
          </button>
        </div>
      </div>
    </div>

    <!-- Notificaciones Toast -->
    <div v-if="notification.show" class="toast-notification" :class="notification.type">
      <div class="toast-content">
        <i :class="getNotificationIcon(notification.type)"></i>
        <span>{{ notification.message }}</span>
      </div>
      <button @click="hideNotification" class="toast-close">
        <i class="fas fa-times"></i>
      </button>
    </div>
  </div>
</template>

<script>
import { ref, computed, onMounted, watch } from 'vue'

export default {
  name: 'PlagiarismDetection',
  
  setup() {
    // Configuración API
    const API_BASE_URL = 'http://localhost:5014' // Ajusta según tu configuración
    const SUPABASE_URL = 'https://nnqbpvbcdwcodnradhye.supabase.co'
    const SUPABASE_KEY = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im5ucWJwdmJjZHdjb2RucmFkaHllIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTg5MzYzMTcsImV4cCI6MjA3NDUxMjMxN30.ZYbcRG9D2J0SlhcT9XTzGX5AAW5wuTXPnzmkbC_pGPU'
    const CODEQUIRY_API_KEY = 'c546d770629c92be10702950f7e5f4b8d7e38d7a0d17fc364b223ea263fe90a4'

    // Estado de la aplicación
    const userEmail = ref('')
    const currentUser = ref({})
    const tenantName = ref('')
    const assignments = ref([])
    const filteredAssignments = ref([])
    const loading = ref(false)
    const selectedAssignmentDetails = ref(null)
    
    // Filtros y búsqueda
    const searchQuery = ref('')
    const selectedAssignment = ref('')
    const selectedStatus = ref('')
    const similarityThreshold = ref('0')
    const sortField = ref('uploaded_at')
    const sortDirection = ref('desc')
    
    // Paginación
    const currentPage = ref(1)
    const itemsPerPage = ref(10)
    
    // Notificaciones
    const notification = ref({
      show: false,
      message: '',
      type: 'info'
    })

    // Computed properties
    const tenantDisplayName = computed(() => {
      switch(tenantName.value) {
        case 'ucb.edu.bo': return 'UCB'
        case 'upb.edu.bo': return 'UPB'
        case 'gmail.com': return 'Gmail'
        default: return tenantName.value
      }
    })

    const totalAssignments = computed(() => assignments.value.length)
    const pendingAnalysis = computed(() => 
      assignments.value.filter(a => a.analysis_status === 'pending').length
    )
    const suspiciousAssignments = computed(() =>
      assignments.value.filter(a => 
        a.analysis_status === 'completed' && a.similarity_score >= 40
      ).length
    )
    const cleanAssignments = computed(() =>
      assignments.value.filter(a => 
        a.analysis_status === 'completed' && a.similarity_score < 40
      ).length
    )
    const hasPendingAssignments = computed(() => pendingAnalysis.value > 0)
    
    const uniqueAssignments = computed(() => {
      const assignmentsSet = new Set()
      assignments.value.forEach(a => {
        if (a.assignment_name) assignmentsSet.add(a.assignment_name)
      })
      return Array.from(assignmentsSet)
    })

    const totalPages = computed(() => 
      Math.ceil(filteredAssignments.value.length / itemsPerPage.value)
    )

    const paginatedAssignments = computed(() => {
      const start = (currentPage.value - 1) * itemsPerPage.value
      const end = start + itemsPerPage.value
      return filteredAssignments.value.slice(start, end)
    })

    // Métodos
    const showNotification = (message, type = 'info') => {
      notification.value = {
        show: true,
        message,
        type
      }
      setTimeout(() => {
        notification.value.show = false
      }, 5000)
    }

    const hideNotification = () => {
      notification.value.show = false
    }

    const getNotificationIcon = (type) => {
      switch(type) {
        case 'success': return 'fas fa-check-circle'
        case 'error': return 'fas fa-exclamation-circle'
        case 'warning': return 'fas fa-exclamation-triangle'
        default: return 'fas fa-info-circle'
      }
    }

    const getTenantFromEmail = (email) => {
      if (email.endsWith('@ucb.edu.bo')) return 'ucb.edu.bo'
      if (email.endsWith('@upb.edu.bo')) return 'upb.edu.bo'
      if (email.endsWith('@gmail.com')) return 'gmail.com'
      return 'unknown'
    }

    const getCurrentUserEmail = async () => {
      try {
        // En un entorno real, obtendrías esto de Supabase Auth
        const { data: { user } } = await supabase.auth.getUser()
        return user?.email || ''
      } catch (error) {
        console.error('Error obteniendo usuario:', error)
        return ''
      }
    }

    const fetchAssignments = async () => {
      loading.value = true
      try {
        // Obtener email del usuario actual
        const email = await getCurrentUserEmail()
        userEmail.value = email
        tenantName.value = getTenantFromEmail(email)

        if (!email) {
          throw new Error('No se pudo obtener el email del usuario')
        }

        // Obtener entregas desde tu API de tareas
        const response = await fetch(`${API_BASE_URL}/api/assignments`, {
          headers: {
            'Authorization': `Bearer ${SUPABASE_KEY}`,
            'X-User-Email': email,
            'X-Tenant': tenantName.value
          }
        })

        if (!response.ok) {
          throw new Error(`Error HTTP: ${response.status}`)
        }

        const data = await response.json()
        assignments.value = data.assignments || []
        filteredAssignments.value = assignments.value
        filterAssignments()
        
        showNotification(`${assignments.value.length} entregas cargadas`, 'success')
        
      } catch (error) {
        console.error('Error cargando entregas:', error)
        showNotification(`Error al cargar entregas: ${error.message}`, 'error')
        // Datos de ejemplo para desarrollo
        loadSampleData()
      } finally {
        loading.value = false
      }
    }

    const loadSampleData = () => {
      assignments.value = [
        {
          id: 1,
          student_id: 101,
          student_name: "Juan Pérez",
          student_email: "juan.perez@ucb.edu.bo",
          assignment_id: 1,
          assignment_name: "Trabajo 1: Algoritmos",
          course_name: "Programación I",
          file_name: "algoritmo_ordenacion.py",
          file_type: "python",
          file_size: 4521,
          file_content: null,
          content_type: "text/x-python",
          is_code_file: true,
          uploaded_at: "2024-01-15T14:30:00Z",
          analysis_status: "completed",
          similarity_score: 87,
          analyzed_at: "2024-01-15T15:45:00Z",
          high_similarity_sources: 3,
          medium_similarity_sources: 2,
          low_similarity_sources: 1,
          similar_sources: [
            {
              source_name: "GitHub - user123/algorithms",
              source_type: "Repositorio público",
              similarity_percentage: 45,
              url: "https://github.com/user123/algorithms",
              matched_lines: 25,
              detected_at: "2024-01-15T15:45:00Z"
            },
            {
              source_name: "Stack Overflow - #453216",
              source_type: "Foro de programación",
              similarity_percentage: 32,
              url: "https://stackoverflow.com/questions/453216",
              matched_lines: 18,
              detected_at: "2024-01-15T15:45:00Z"
            }
          ]
        },
        {
          id: 2,
          student_id: 102,
          student_name: "María García",
          student_email: "maria.garcia@ucb.edu.bo",
          assignment_id: 1,
          assignment_name: "Trabajo 1: Algoritmos",
          course_name: "Programación I",
          file_name: "ordenacion_rapida.cpp",
          file_type: "cpp",
          file_size: 3210,
          file_content: null,
          content_type: "text/x-c++",
          is_code_file: true,
          uploaded_at: "2024-01-16T10:15:00Z",
          analysis_status: "completed",
          similarity_score: 12,
          analyzed_at: "2024-01-16T11:30:00Z",
          high_similarity_sources: 0,
          medium_similarity_sources: 0,
          low_similarity_sources: 1,
          similar_sources: []
        },
        {
          id: 3,
          student_id: 103,
          student_name: "Carlos López",
          student_email: "carlos.lopez@ucb.edu.bo",
          assignment_id: 2,
          assignment_name: "Trabajo 2: Estructuras de datos",
          course_name: "Estructuras de Datos",
          file_name: "arbol_binario.java",
          file_type: "java",
          file_size: 5890,
          file_content: null,
          content_type: "text/x-java",
          is_code_file: true,
          uploaded_at: "2024-01-20T16:45:00Z",
          analysis_status: "pending",
          similarity_score: null,
          analyzed_at: null
        }
      ]
      filteredAssignments.value = assignments.value
      showNotification("Usando datos de ejemplo. Conecta tu backend para datos reales.", "warning")
    }

    const filterAssignments = () => {
      let filtered = assignments.value

      // Filtrar por búsqueda
      if (searchQuery.value) {
        const query = searchQuery.value.toLowerCase()
        filtered = filtered.filter(a => 
          a.student_name.toLowerCase().includes(query) ||
          a.student_email.toLowerCase().includes(query) ||
          a.file_name.toLowerCase().includes(query) ||
          a.assignment_name.toLowerCase().includes(query)
        )
      }

      // Filtrar por trabajo
      if (selectedAssignment.value) {
        filtered = filtered.filter(a => a.assignment_name === selectedAssignment.value)
      }

      // Filtrar por estado
      if (selectedStatus.value) {
        filtered = filtered.filter(a => a.analysis_status === selectedStatus.value)
      }

      // Filtrar por umbral de similitud
      if (similarityThreshold.value !== '0') {
        const threshold = parseInt(similarityThreshold.value)
        filtered = filtered.filter(a => 
          a.analysis_status === 'completed' && 
          a.similarity_score >= threshold
        )
      }

      // Ordenar
      filtered.sort((a, b) => {
        let aVal = a[sortField.value]
        let bVal = b[sortField.value]

        if (sortField.value === 'similarity_score') {
          aVal = aVal || 0
          bVal = bVal || 0
        }

        if (sortDirection.value === 'asc') {
          return aVal > bVal ? 1 : -1
        } else {
          return aVal < bVal ? 1 : -1
        }
      })

      filteredAssignments.value = filtered
      currentPage.value = 1 // Resetear a primera página
    }

    const sortBy = (field) => {
      if (sortField.value === field) {
        sortDirection.value = sortDirection.value === 'asc' ? 'desc' : 'asc'
      } else {
        sortField.value = field
        sortDirection.value = 'desc'
      }
      filterAssignments()
    }

    const filterByStatus = (status) => {
      selectedStatus.value = status === 'all' ? '' : status
      filterAssignments()
    }

    const getStatusDisplay = (status) => {
      const statusMap = {
        pending: 'Pendiente',
        analyzing: 'Analizando',
        completed: 'Completado',
        error: 'Error'
      }
      return statusMap[status] || status
    }

    const getStatusClass = (status) => {
      return `status-${status}`
    }

    const getSimilarityClass = (score) => {
      if (score >= 70) return 'similarity-high'
      if (score >= 40) return 'similarity-medium'
      if (score >= 20) return 'similarity-low'
      return 'similarity-none'
    }

    const getFileIcon = (fileType) => {
      const icons = {
        python: 'fab fa-python',
        java: 'fab fa-java',
        cpp: 'fas fa-file-code',
        js: 'fab fa-js-square',
        html: 'fab fa-html5',
        css: 'fab fa-css3-alt',
        sql: 'fas fa-database',
        txt: 'fas fa-file-alt',
        docx: 'fas fa-file-word',
        pdf: 'fas fa-file-pdf'
      }
      return icons[fileType] || 'fas fa-file'
    }

    const formatFileSize = (bytes) => {
      if (bytes < 1024) return bytes + ' B'
      if (bytes < 1048576) return (bytes / 1024).toFixed(1) + ' KB'
      return (bytes / 1048576).toFixed(1) + ' MB'
    }

    const formatDate = (dateString) => {
      if (!dateString) return 'N/A'
      const date = new Date(dateString)
      return date.toLocaleDateString('es-ES', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
      })
    }

    const formatRelativeTime = (dateString) => {
      const date = new Date(dateString)
      const now = new Date()
      const diffMs = now - date
      const diffMins = Math.floor(diffMs / 60000)
      const diffHours = Math.floor(diffMs / 3600000)
      const diffDays = Math.floor(diffMs / 86400000)

      if (diffMins < 60) return `Hace ${diffMins} min`
      if (diffHours < 24) return `Hace ${diffHours} h`
      if (diffDays < 7) return `Hace ${diffDays} d`
      return formatDate(dateString)
    }

    const getRowClass = (assignment) => {
      if (assignment.analysis_status === 'completed') {
        return getSimilarityClass(assignment.similarity_score)
      }
      return assignment.analysis_status
    }

    const analyzeAssignment = async (assignment) => {
      try {
        assignment.analyzing = true
        
        // Llamar a tu API de análisis de plagio
        const response = await fetch(`${API_BASE_URL}/api/analyze`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${SUPABASE_KEY}`,
            'X-User-Email': userEmail.value
          },
          body: JSON.stringify({
            assignment_id: assignment.id,
            file_content: assignment.file_content,
            file_name: assignment.file_name,
            file_type: assignment.file_type,
            codequiry_api_key: CODEQUIRY_API_KEY
          })
        })

        if (!response.ok) {
          throw new Error(`Error HTTP: ${response.status}`)
        }

        const result = await response.json()
        
        // Actualizar assignment localmente
        Object.assign(assignment, {
          analysis_status: 'completed',
          similarity_score: result.similarity_score,
          analyzed_at: new Date().toISOString(),
          similar_sources: result.similar_sources || [],
          analyzing: false
        })

        showNotification(`Análisis completado para ${assignment.file_name}: ${result.similarity_score}% de similitud`, 'success')
        
      } catch (error) {
        console.error('Error analizando assignment:', error)
        assignment.analysis_status = 'error'
        assignment.analyzing = false
        showNotification(`Error al analizar: ${error.message}`, 'error')
      }
    }

    const analyzeAllPending = async () => {
      const pending = assignments.value.filter(a => a.analysis_status === 'pending')
      
      if (pending.length === 0) {
        showNotification('No hay entregas pendientes de análisis', 'info')
        return
      }

      showNotification(`Iniciando análisis de ${pending.length} entregas...`, 'info')

      for (const assignment of pending) {
        await analyzeAssignment(assignment)
        await new Promise(resolve => setTimeout(resolve, 1000)) // Pausa entre análisis
      }

      showNotification('Análisis completado para todas las entregas pendientes', 'success')
    }

    const viewDetails = (assignment) => {
      selectedAssignmentDetails.value = assignment
    }

    const closeModal = () => {
      selectedAssignmentDetails.value = null
    }

    const retryAnalysis = (assignment) => {
      assignment.analysis_status = 'pending'
      analyzeAssignment(assignment)
    }

    const downloadFile = async (assignment) => {
      try {
        // En un entorno real, esto descargaría el archivo desde tu API
        const response = await fetch(`${API_BASE_URL}/api/download/${assignment.id}`, {
          headers: {
            'Authorization': `Bearer ${SUPABASE_KEY}`,
            'X-User-Email': userEmail.value
          }
        })

        if (!response.ok) {
          throw new Error(`Error HTTP: ${response.status}`)
        }

        const blob = await response.blob()
        const url = window.URL.createObjectURL(blob)
        const a = document.createElement('a')
        a.href = url
        a.download = assignment.file_name
        document.body.appendChild(a)
        a.click()
        window.URL.revokeObjectURL(url)
        document.body.removeChild(a)

        showNotification(`Archivo ${assignment.file_name} descargado`, 'success')
        
      } catch (error) {
        console.error('Error descargando archivo:', error)
        showNotification(`Error al descargar archivo: ${error.message}`, 'error')
      }
    }

    const downloadReport = (assignment) => {
      // Generar reporte PDF del análisis
      const reportContent = `
        REPORTE DE ANÁLISIS DE PLAGIO
        ==============================
        
        Estudiante: ${assignment.student_name}
        Email: ${assignment.student_email}
        Trabajo: ${assignment.assignment_name}
        Archivo: ${assignment.file_name}
        Fecha de subida: ${formatDate(assignment.uploaded_at)}
        Fecha de análisis: ${formatDate(assignment.analyzed_at)}
        
        RESULTADOS:
        ----------
        Similitud total: ${assignment.similarity_score}%
        
        Fuentes similares detectadas:
        ${assignment.similar_sources?.map(source => 
          `- ${source.source_name}: ${source.similarity_percentage}%`
        ).join('\n') || 'No se encontraron fuentes similares'}
        
        Instituto: ${tenantDisplayName.value}
        Generado: ${new Date().toLocaleDateString('es-ES')}
      `

      const blob = new Blob([reportContent], { type: 'text/plain' })
      const url = window.URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `reporte-plagio-${assignment.file_name}.txt`
      document.body.appendChild(a)
      a.click()
      window.URL.revokeObjectURL(url)
      document.body.removeChild(a)

      showNotification('Reporte descargado', 'success')
    }

    const exportToCSV = () => {
      const headers = ['Estudiante', 'Email', 'Trabajo', 'Archivo', 'Fecha', 'Similitud', 'Estado']
      const csvData = assignments.value.map(a => [
        a.student_name,
        a.student_email,
        a.assignment_name,
        a.file_name,
        formatDate(a.uploaded_at),
        a.similarity_score || 'N/A',
        getStatusDisplay(a.analysis_status)
      ])

      const csvContent = [
        headers.join(','),
        ...csvData.map(row => row.join(','))
      ].join('\n')

      const blob = new Blob([csvContent], { type: 'text/csv' })
      const url = window.URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `plagio-${tenantDisplayName.value}-${new Date().toISOString().split('T')[0]}.csv`
      document.body.appendChild(a)
      a.click()
      window.URL.revokeObjectURL(url)
      document.body.removeChild(a)

      showNotification('Datos exportados a CSV', 'success')
    }

    const refreshAssignments = () => {
      fetchAssignments()
    }

    const prevPage = () => {
      if (currentPage.value > 1) {
        currentPage.value--
      }
    }

    const nextPage = () => {
      if (currentPage.value < totalPages.value) {
        currentPage.value++
      }
    }

    const logout = () => {
      // Implementar lógica de logout
      showNotification('Sesión cerrada', 'info')
      window.location.href = '/login'
    }

    // Lifecycle hooks
    onMounted(() => {
      fetchAssignments()
    })

    // Watch para cambios en assignments
    watch(assignments, () => {
      filterAssignments()
    }, { deep: true })

    return {
      // Data
      userEmail,
      currentUser,
      tenantName,
      assignments,
      filteredAssignments: paginatedAssignments,
      loading,
      selectedAssignmentDetails,
      searchQuery,
      selectedAssignment,
      selectedStatus,
      similarityThreshold,
      sortField,
      sortDirection,
      currentPage,
      itemsPerPage,
      notification,
      
      // Computed
      tenantDisplayName,
      totalAssignments,
      pendingAnalysis,
      suspiciousAssignments,
      cleanAssignments,
      hasPendingAssignments,
      uniqueAssignments,
      totalPages,
      
      // Methods
      showNotification,
      hideNotification,
      getNotificationIcon,
      fetchAssignments,
      filterAssignments,
      sortBy,
      filterByStatus,
      getStatusDisplay,
      getStatusClass,
      getSimilarityClass,
      getFileIcon,
      formatFileSize,
      formatDate,
      formatRelativeTime,
      getRowClass,
      analyzeAssignment,
      analyzeAllPending,
      viewDetails,
      closeModal,
      retryAnalysis,
      downloadFile,
      downloadReport,
      exportToCSV,
      refreshAssignments,
      prevPage,
      nextPage,
      logout
    }
  }
}
</script>

<style scoped>
/* Estilos completos del sistema */
.plagiarism-dashboard {
  min-height: 100vh;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  padding: 20px;
  font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
}

/* Header */
.dashboard-header {
  background: rgba(255, 255, 255, 0.95);
  border-radius: 16px;
  padding: 24px 32px;
  margin-bottom: 24px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.1);
  backdrop-filter: blur(10px);
}

.header-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 20px;
}

.header-left h1 {
  color: #2d3748;
  font-size: 2rem;
  font-weight: 700;
  margin: 0;
  display: flex;
  align-items: center;
  gap: 12px;
}

.header-left h1 i {
  color: #667eea;
}

.subtitle {
  color: #718096;
  margin: 8px 0 0;
  font-size: 1rem;
  display: flex;
  align-items: center;
  gap: 8px;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 12px 20px;
  background: #f7fafc;
  border-radius: 12px;
  border: 1px solid #e2e8f0;
}

.avatar {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  background: linear-gradient(135deg, #667eea, #764ba2);
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-size: 1.2rem;
}

.user-details {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.user-name {
  font-weight: 600;
  color: #2d3748;
  font-size: 1.1rem;
}

.user-email {
  color: #718096;
  font-size: 0.9rem;
}

.user-role {
  background: #667eea;
  color: white;
  padding: 4px 12px;
  border-radius: 20px;
  font-size: 0.8rem;
  font-weight: 600;
  align-self: flex-start;
}

/* Panel de Estadísticas */
.stats-panel {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
  gap: 20px;
  margin-bottom: 24px;
}

.stat-card {
  background: white;
  border-radius: 16px;
  padding: 24px;
  display: flex;
  align-items: center;
  gap: 20px;
  cursor: pointer;
  transition: all 0.3s ease;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
  border: 2px solid transparent;
}

.stat-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 12px 24px rgba(0, 0, 0, 0.15);
  border-color: #667eea;
}

.stat-icon {
  width: 64px;
  height: 64px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.8rem;
  color: white;
}

.stat-icon.total { background: linear-gradient(135deg, #4c6ef5, #3b5bdb); }
.stat-icon.pending { background: linear-gradient(135deg, #ffd43b, #fab005); }
.stat-icon.suspicious { background: linear-gradient(135deg, #fa5252, #e03131); }
.stat-icon.clean { background: linear-gradient(135deg, #51cf66, #2b8a3e); }

.stat-info h3 {
  color: #2d3748;
  font-size: 2rem;
  font-weight: 700;
  margin: 0;
  line-height: 1;
}

.stat-info p {
  color: #718096;
  margin: 8px 0 0;
  font-size: 0.9rem;
  font-weight: 500;
}

/* Panel de Controles */
.control-panel {
  background: white;
  border-radius: 16px;
  padding: 24px;
  margin-bottom: 24px;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
}

.search-section {
  display: flex;
  flex-wrap: wrap;
  gap: 20px;
  margin-bottom: 20px;
}

.search-box {
  flex: 1;
  min-width: 300px;
  position: relative;
}

.search-box i {
  position: absolute;
  left: 16px;
  top: 50%;
  transform: translateY(-50%);
  color: #a0aec0;
}

.search-input {
  width: 100%;
  padding: 14px 20px 14px 48px;
  border: 2px solid #e2e8f0;
  border-radius: 12px;
  font-size: 1rem;
  transition: all 0.3s ease;
  color: #2d3748;
}

.search-input:focus {
  outline: none;
  border-color: #667eea;
  box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
}

.filter-group {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
}

.filter-select {
  padding: 12px 20px;
  border: 2px solid #e2e8f0;
  border-radius: 12px;
  font-size: 0.95rem;
  color: #2d3748;
  background: white;
  cursor: pointer;
  min-width: 160px;
}

.filter-select:focus {
  outline: none;
  border-color: #667eea;
}

.action-buttons {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
}

.btn {
  padding: 14px 28px;
  border: none;
  border-radius: 12px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
  display: flex;
  align-items: center;
  gap: 10px;
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-refresh {
  background: #f7fafc;
  color: #2d3748;
  border: 2px solid #e2e8f0;
}

.btn-refresh:hover:not(:disabled) {
  background: #edf2f7;
  border-color: #cbd5e0;
}

.btn-analyze-all {
  background: linear-gradient(135deg, #667eea, #764ba2);
  color: white;
}

.btn-analyze-all:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 8px 16px rgba(102, 126, 234, 0.3);
}

.btn-export {
  background: #38a169;
  color: white;
}

.btn-export:hover:not(:disabled) {
  background: #2f855a;
  transform: translateY(-2px);
}

/* Tabla de Entregas */
.assignments-table {
  background: white;
  border-radius: 16px;
  overflow: hidden;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
}

.table-header {
  padding: 24px 32px;
  border-bottom: 1px solid #e2e8f0;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 16px;
}

.table-header h3 {
  color: #2d3748;
  margin: 0;
  font-size: 1.3rem;
}

.table-actions {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.filter-tag {
  background: #e2e8f0;
  color: #2d3748;
  padding: 6px 12px;
  border-radius: 20px;
  font-size: 0.85rem;
  display: flex;
  align-items: center;
  gap: 6px;
}

.tag-remove {
  background: none;
  border: none;
  color: #718096;
  cursor: pointer;
  padding: 2px;
  font-size: 0.9rem;
}

.tag-remove:hover {
  color: #e53e3e;
}

.loading-container {
  padding: 60px 20px;
  text-align: center;
}

.spinner {
  width: 48px;
  height: 48px;
  border: 4px solid #f3f3f3;
  border-top: 4px solid #667eea;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin: 0 auto 20px;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.empty-state {
  padding: 60px 20px;
  text-align: center;
  color: #a0aec0;
}

.empty-state i {
  font-size: 3rem;
  margin-bottom: 20px;
  opacity: 0.5;
}

.empty-state h4 {
  color: #718096;
  margin: 0 0 8px;
}

.empty-state p {
  margin: 0;
  font-size: 0.95rem;
}

.table-responsive {
  overflow-x: auto;
}

.assignments-list {
  width: 100%;
  border-collapse: collapse;
}

.assignments-list thead {
  background: #f7fafc;
  border-bottom: 2px solid #e2e8f0;
}

.assignments-list th {
  padding: 20px 16px;
  text-align: left;
  font-weight: 600;
  color: #4a5568;
  font-size: 0.9rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  white-space: nowrap;
}

.assignments-list td {
  padding: 20px 16px;
  border-bottom: 1px solid #e2e8f0;
  vertical-align: middle;
}

.assignments-list tr:hover {
  background: #f7fafc;
}

.assignments-list tr.similarity-high {
  background: #fff5f5;
}

.assignments-list tr.similarity-medium {
  background: #fffbf0;
}

.assignments-list tr.similarity-low {
  background: #f0fff4;
}

.assignments-list tr.similarity-none {
  background: #f7fafc;
}

.assignments-list tr.pending {
  background: #fffaf0;
}

.assignments-list tr.analyzing {
  background: #f0f9ff;
}

.assignments-list tr.error {
  background: #fff5f5;
}

.sortable {
  cursor: pointer;
  user-select: none;
  transition: color 0.3s ease;
}

.sortable:hover {
  color: #667eea;
}

.sortable i {
  margin-left: 8px;
  font-size: 0.8rem;
}

.student-cell {
  min-width: 200px;
}

.student-info {
  display: flex;
  align-items: center;
  gap: 12px;
}

.student-avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: #e2e8f0;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #667eea;
  flex-shrink: 0;
}

.student-details {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.student-details strong {
  color: #2d3748;
  font-size: 1rem;
}

.student-details small {
  color: #718096;
  font-size: 0.85rem;
}

.assignment-cell, .file-cell {
  min-width: 180px;
}

.assignment-info, .file-info {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.file-info {
  display: flex;
  align-items: center;
  gap: 12px;
}

.file-info i {
  font-size: 1.2rem;
  color: #667eea;
  width: 24px;
  flex-shrink: 0;
}

.date-cell, .similarity-cell, .status-cell, .actions-cell {
  min-width: 120px;
}

.similarity-display {
  display: flex;
  align-items: center;
  gap: 12px;
}

.similarity-score {
  font-weight: 700;
  font-size: 1.1rem;
  min-width: 50px;
  text-align: center;
}

.similarity-score.similarity-high { color: #e53e3e; }
.similarity-score.similarity-medium { color: #d69e2e; }
.similarity-score.similarity-low { color: #38a169; }
.similarity-score.similarity-none { color: #718096; }

.similarity-bar {
  flex: 1;
  height: 8px;
  background: #e2e8f0;
  border-radius: 4px;
  overflow: hidden;
}

.similarity-fill {
  height: 100%;
  border-radius: 4px;
}

.similarity-fill.similarity-high { background: #e53e3e; }
.similarity-fill.similarity-medium { background: #d69e2e; }
.similarity-fill.similarity-low { background: #38a169; }
.similarity-fill.similarity-none { background: #718096; }

.similarity-placeholder {
  color: #a0aec0;
  font-style: italic;
}

.status-badge {
  display: inline-block;
  padding: 6px 12px;
  border-radius: 20px;
  font-size: 0.85rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.status-pending {
  background: #fffaf0;
  color: #d69e2e;
  border: 1px solid #f6ad55;
}

.status-analyzing {
  background: #f0f9ff;
  color: #3182ce;
  border: 1px solid #90cdf4;
}

.status-completed {
  background: #f0fff4;
  color: #38a169;
  border: 1px solid #9ae6b4;
}

.status-error {
  background: #fff5f5;
  color: #e53e3e;
  border: 1px solid #fc8181;
}

.status-time {
  display: block;
  color: #a0aec0;
  font-size: 0.8rem;
  margin-top: 4px;
}

.actions-cell {
  display: flex;
  gap: 8px;
}

.btn-action {
  padding: 8px 16px;
  border: none;
  border-radius: 8px;
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
  display: flex;
  align-items: center;
  gap: 6px;
  white-space: nowrap;
}

.analyze-btn {
  background: #667eea;
  color: white;
}

.analyze-btn:hover:not(:disabled) {
  background: #5a67d8;
}

.details-btn {
  background: #38a169;
  color: white;
}

.details-btn:hover {
  background: #2f855a;
}

.retry-btn {
  background: #ed8936;
  color: white;
}

.retry-btn:hover {
  background: #dd6b20;
}

.download-btn {
  background: #e2e8f0;
  color: #2d3748;
  padding: 8px;
}

.download-btn:hover {
  background: #cbd5e0;
}

/* Paginación */
.pagination {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 20px;
  padding: 24px;
  background: white;
  border-radius: 16px;
  margin-top: 24px;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
}

.page-btn {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  border: 2px solid #e2e8f0;
  background: white;
  color: #4a5568;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.3s ease;
}

.page-btn:hover:not(:disabled) {
  border-color: #667eea;
  color: #667eea;
}

.page-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.page-info {
  color: #4a5568;
  font-weight: 600;
}

/* Modal */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.7);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 20px;
  backdrop-filter: blur(4px);
}

.modal-content {
  background: white;
  border-radius: 24px;
  width: 100%;
  max-width: 800px;
  max-height: 90vh;
  overflow-y: auto;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
  animation: modalSlideIn 0.3s ease;
}

@keyframes modalSlideIn {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.modal-header {
  padding: 32px 32px 24px;
  border-bottom: 1px solid #e2e8f0;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.modal-header h3 {
  color: #2d3748;
  margin: 0;
  font-size: 1.5rem;
  display: flex;
  align-items: center;
  gap: 12px;
}

.modal-close {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  border: 2px solid #e2e8f0;
  background: white;
  color: #718096;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.2rem;
  transition: all 0.3s ease;
}

.modal-close:hover {
  border-color: #e53e3e;
  color: #e53e3e;
}

.modal-body {
  padding: 24px 32px;
}

.detail-section {
  margin-bottom: 32px;
}

.detail-section h4 {
  color: #2d3748;
  font-size: 1.2rem;
  margin: 0 0 20px;
  padding-bottom: 12px;
  border-bottom: 2px solid #e2e8f0;
}

.file-details-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  gap: 20px;
}

.detail-item {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.detail-item label {
  color: #718096;
  font-size: 0.9rem;
  font-weight: 600;
}

.detail-item span {
  color: #2d3748;
  font-size: 1rem;
}

.similarity-summary {
  display: flex;
  align-items: center;
  gap: 40px;
  flex-wrap: wrap;
}

.similarity-main {
  text-align: center;
}

.similarity-score-large {
  font-size: 3.5rem;
  font-weight: 800;
  line-height: 1;
  margin-bottom: 8px;
}

.similarity-label {
  color: #718096;
  font-size: 0.95rem;
}

.similarity-breakdown {
  flex: 1;
  min-width: 200px;
}

.breakdown-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px;
  margin-bottom: 8px;
  border-radius: 8px;
  font-weight: 600;
}

.breakdown-item.high {
  background: #fff5f5;
  color: #e53e3e;
}

.breakdown-item.medium {
  background: #fffaf0;
  color: #d69e2e;
}

.breakdown-item.low {
  background: #f0fff4;
  color: #38a169;
}

.breakdown-label {
  font-size: 0.95rem;
}

.breakdown-value {
  font-size: 1.1rem;
}

.sources-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.source-item {
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  padding: 20px;
  transition: all 0.3s ease;
}

.source-item:hover {
  border-color: #cbd5e0;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.05);
}

.source-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 16px;
}

.source-info {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.source-info strong {
  color: #2d3748;
  font-size: 1.1rem;
}

.source-info small {
  color: #718096;
  font-size: 0.85rem;
}

.source-similarity {
  padding: 6px 12px;
  border-radius: 20px;
  font-weight: 700;
  font-size: 0.9rem;
  min-width: 60px;
  text-align: center;
}

.source-details {
  color: #4a5568;
  font-size: 0.9rem;
}

.source-details p {
  margin: 8px 0;
  display: flex;
  align-items: center;
  gap: 8px;
}

.source-details a {
  color: #667eea;
  text-decoration: none;
}

.source-details a:hover {
  text-decoration: underline;
}

.no-sources {
  text-align: center;
  padding: 40px 20px;
  color: #a0aec0;
}

.no-sources i {
  font-size: 3rem;
  margin-bottom: 16px;
  opacity: 0.5;
}

.tech-info {
  background: #f7fafc;
  border-radius: 12px;
  padding: 20px;
}

.tech-info p {
  margin: 8px 0;
  display: flex;
  align-items: center;
  gap: 12px;
  color: #4a5568;
}

.tech-info i {
  color: #667eea;
  width: 20px;
}

.modal-footer {
  padding: 24px 32px;
  border-top: 1px solid #e2e8f0;
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}

.btn-close {
  background: #e2e8f0;
  color: #2d3748;
}

.btn-close:hover {
  background: #cbd5e0;
}

/* Toast Notifications */
.toast-notification {
  position: fixed;
  bottom: 24px;
  right: 24px;
  min-width: 300px;
  max-width: 400px;
  padding: 16px 20px;
  border-radius: 12px;
  box-shadow: 0 10px 25px rgba(0, 0, 0, 0.2);
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  z-index: 1001;
  animation: toastSlideIn 0.3s ease;
}

@keyframes toastSlideIn {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.toast-notification.success {
  background: #38a169;
  color: white;
  border-left: 4px solid #2f855a;
}

.toast-notification.error {
  background: #e53e3e;
  color: white;
  border-left: 4px solid #c53030;
}

.toast-notification.warning {
  background: #d69e2e;
  color: white;
  border-left: 4px solid #b7791f;
}

.toast-notification.info {
  background: #3182ce;
  color: white;
  border-left: 4px solid #2b6cb0;
}

.toast-content {
  display: flex;
  align-items: center;
  gap: 12px;
  flex: 1;
}

.toast-content i {
  font-size: 1.2rem;
}

.toast-close {
  background: none;
  border: none;
  color: white;
  opacity: 0.8;
  cursor: pointer;
  padding: 4px;
  font-size: 1.1rem;
}

.toast-close:hover {
  opacity: 1;
}

/* Responsive Design */
@media (max-width: 1024px) {
  .stats-panel {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 768px) {
  .dashboard-header {
    padding: 20px;
  }
  
  .header-content {
    flex-direction: column;
    align-items: flex-start;
  }
  
  .header-left h1 {
    font-size: 1.6rem;
  }
  
  .stats-panel {
    grid-template-columns: 1fr;
  }
  
  .search-box {
    min-width: 100%;
  }
  
  .filter-group {
    width: 100%;
  }
  
  .filter-select {
    flex: 1;
    min-width: 0;
  }
  
  .action-buttons {
    width: 100%;
  }
  
  .btn {
    flex: 1;
    justify-content: center;
  }
  
  .table-header {
    flex-direction: column;
    align-items: flex-start;
  }
  
  .assignments-list th,
  .assignments-list td {
    padding: 16px 12px;
  }
  
  .actions-cell {
    flex-direction: column;
  }
  
  .btn-action {
    width: 100%;
    justify-content: center;
  }
  
  .modal-content {
    margin: 20px;
    max-height: calc(100vh - 40px);
  }
  
  .similarity-summary {
    flex-direction: column;
    align-items: stretch;
    gap: 20px;
  }
}

@media (max-width: 480px) {
  .plagiarism-dashboard {
    padding: 12px;
  }
  
  .dashboard-header,
  .control-panel,
  .assignments-table,
  .pagination {
    border-radius: 12px;
    padding: 16px;
  }
  
  .header-left h1 {
    font-size: 1.4rem;
  }
  
  .user-info {
    width: 100%;
    justify-content: center;
  }
  
  .modal-header,
  .modal-body,
  .modal-footer {
    padding: 16px;
  }
  
  .file-details-grid {
    grid-template-columns: 1fr;
  }
  
  .toast-notification {
    left: 12px;
    right: 12px;
    min-width: 0;
    width: calc(100% - 24px);
  }
}
</style>