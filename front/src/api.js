// api.js - Servicio para manejar todas las peticiones API
import { supabase } from './supabase'

const API_BASE_URL = import.meta.env.VITE_TAREAS_API || 'http://localhost:5011'
const CODEQUIRY_API_KEY = 'c546d770629c92be10702950f7e5f4b8d7e38d7a0d17fc364b223ea263fe90a4'

export const apiService = {
  // Obtener todas las entregas
  async getAssignments() {
    try {
      const { data: { user } } = await supabase.auth.getUser()
      if (!user?.email) throw new Error('Usuario no autenticado')

      const response = await fetch(`${API_BASE_URL}/api/assignments`, {
        headers: {
          'X-User-Email': user.email,
          'Authorization': `Bearer ${supabase.auth.session()?.access_token}`
        }
      })

      if (!response.ok) throw new Error(`HTTP ${response.status}`)
      return await response.json()
    } catch (error) {
      console.error('Error obteniendo entregas:', error)
      throw error
    }
  },

  // Analizar una entrega con Codequiry
  async analyzeAssignment(assignmentId, fileContent, fileName, fileType) {
    try {
      const { data: { user } } = await supabase.auth.getUser()
      if (!user?.email) throw new Error('Usuario no autenticado')

      const response = await fetch(`${API_BASE_URL}/api/analyze`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-User-Email': user.email,
          'Authorization': `Bearer ${supabase.auth.session()?.access_token}`
        },
        body: JSON.stringify({
          assignment_id: assignmentId,
          file_content: fileContent,
          file_name: fileName,
          file_type: fileType,
          codequiry_api_key: CODEQUIRY_API_KEY
        })
      })

      if (!response.ok) throw new Error(`HTTP ${response.status}`)
      return await response.json()
    } catch (error) {
      console.error('Error analizando entrega:', error)
      throw error
    }
  },

  // Descargar archivo
  async downloadFile(assignmentId) {
    try {
      const { data: { user } } = await supabase.auth.getUser()
      if (!user?.email) throw new Error('Usuario no autenticado')

      const response = await fetch(`${API_BASE_URL}/api/download/${assignmentId}`, {
        headers: {
          'X-User-Email': user.email,
          'Authorization': `Bearer ${supabase.auth.session()?.access_token}`
        }
      })

      if (!response.ok) throw new Error(`HTTP ${response.status}`)
      return await response.blob()
    } catch (error) {
      console.error('Error descargando archivo:', error)
      throw error
    }
  },

  // Obtener detalles completos de una entrega
  async getAssignmentDetails(assignmentId) {
    try {
      const { data: { user } } = await supabase.auth.getUser()
      if (!user?.email) throw new Error('Usuario no autenticado')

      const response = await fetch(`${API_BASE_URL}/api/assignments/${assignmentId}/details`, {
        headers: {
          'X-User-Email': user.email,
          'Authorization': `Bearer ${supabase.auth.session()?.access_token}`
        }
      })

      if (!response.ok) throw new Error(`HTTP ${response.status}`)
      return await response.json()
    } catch (error) {
      console.error('Error obteniendo detalles:', error)
      throw error
    }
  },

  // Obtener trabajos (assignments) disponibles
  async getAvailableAssignments() {
    try {
      const { data: { user } } = await supabase.auth.getUser()
      if (!user?.email) throw new Error('Usuario no autenticado')

      const response = await fetch(`${API_BASE_URL}/api/courses/assignments`, {
        headers: {
          'X-User-Email': user.email,
          'Authorization': `Bearer ${supabase.auth.session()?.access_token}`
        }
      })

      if (!response.ok) throw new Error(`HTTP ${response.status}`)
      return await response.json()
    } catch (error) {
      console.error('Error obteniendo trabajos:', error)
      throw error
    }
  },

  // Generar reporte PDF
  async generateReport(assignmentId) {
    try {
      const { data: { user } } = await supabase.auth.getUser()
      if (!user?.email) throw new Error('Usuario no autenticado')

      const response = await fetch(`${API_BASE_URL}/api/reports/generate/${assignmentId}`, {
        headers: {
          'X-User-Email': user.email,
          'Authorization': `Bearer ${supabase.auth.session()?.access_token}`
        }
      })

      if (!response.ok) throw new Error(`HTTP ${response.status}`)
      return await response.blob()
    } catch (error) {
      console.error('Error generando reporte:', error)
      throw error
    }
  },

  // Obtener estadísticas del curso
  async getCourseStats(courseId) {
    try {
      const { data: { user } } = await supabase.auth.getUser()
      if (!user?.email) throw new Error('Usuario no autenticado')

      const response = await fetch(`${API_BASE_URL}/api/courses/${courseId}/stats`, {
        headers: {
          'X-User-Email': user.email,
          'Authorization': `Bearer ${supabase.auth.session()?.access_token}`
        }
      })

      if (!response.ok) throw new Error(`HTTP ${response.status}`)
      return await response.json()
    } catch (error) {
      console.error('Error obteniendo estadísticas:', error)
      throw error
    }
  },

  // Marcar entrega como revisada
  async markAsReviewed(assignmentId) {
    try {
      const { data: { user } } = await supabase.auth.getUser()
      if (!user?.email) throw new Error('Usuario no autenticado')

      const response = await fetch(`${API_BASE_URL}/api/assignments/${assignmentId}/review`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-User-Email': user.email,
          'Authorization': `Bearer ${supabase.auth.session()?.access_token}`
        },
        body: JSON.stringify({
          reviewed_at: new Date().toISOString(),
          reviewed_by: user.email
        })
      })

      if (!response.ok) throw new Error(`HTTP ${response.status}`)
      return await response.json()
    } catch (error) {
      console.error('Error marcando como revisado:', error)
      throw error
    }
  }
}