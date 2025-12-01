<template>
  <div class="submissions-list-page">
    <div class="container mx-auto px-4 py-8">
      <div class="max-w-6xl mx-auto">
        <!-- Header -->
        <div class="mb-8">
          <h1 class="text-3xl font-bold text-gray-800 mb-2">Code Submissions</h1>
          <p class="text-gray-600">View and manage all code submissions</p>
        </div>

        <!-- Filters -->
        <div class="bg-white rounded-lg shadow-md p-6 mb-6">
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label class="block text-gray-700 font-medium mb-2">Assignment ID</label>
              <input
                v-model="filters.assignmentId"
                type="text"
                placeholder="Enter assignment ID"
                class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              />
            </div>
            <div>
              <label class="block text-gray-700 font-medium mb-2">Student ID (Optional)</label>
              <input
                v-model="filters.studentId"
                type="text"
                placeholder="Enter student ID"
                class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              />
            </div>
          </div>
          <button
            @click="loadSubmissions"
            :disabled="!filters.assignmentId || isLoading"
            class="mt-4 bg-blue-500 text-white px-6 py-2 rounded-lg hover:bg-blue-600 transition-colors disabled:bg-gray-400 disabled:cursor-not-allowed"
          >
            <span v-if="!isLoading">Load Submissions</span>
            <span v-else>Loading...</span>
          </button>
        </div>

        <!-- Submissions Table -->
        <div v-if="submissions.length > 0" class="bg-white rounded-lg shadow-md overflow-hidden">
          <div class="overflow-x-auto">
            <table class="min-w-full divide-y divide-gray-200">
              <thead class="bg-gray-50">
                <tr>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Student ID
                  </th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Filename
                  </th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Language
                  </th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Status
                  </th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Submitted
                  </th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody class="bg-white divide-y divide-gray-200">
                <tr v-for="submission in submissions" :key="submission.id" class="hover:bg-gray-50">
                  <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                    {{ submission.student_id }}
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    {{ submission.filename }}
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    <span class="px-2 py-1 bg-blue-100 text-blue-800 rounded text-xs">
                      {{ submission.file_extension.toUpperCase() }}
                    </span>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap text-sm">
                    <span v-if="submission.is_analyzed && submission.analysis_result?.is_valid" class="px-2 py-1 bg-green-100 text-green-800 rounded text-xs">
                      ✓ Valid
                    </span>
                    <span v-else-if="submission.is_analyzed && !submission.analysis_result?.is_valid" class="px-2 py-1 bg-red-100 text-red-800 rounded text-xs">
                      ✗ Errors
                    </span>
                    <span v-else class="px-2 py-1 bg-gray-100 text-gray-800 rounded text-xs">
                      Pending
                    </span>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    {{ formatDate(submission.submitted_at) }}
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap text-sm font-medium">
                    <button
                      @click="viewSubmission(submission)"
                      class="text-blue-600 hover:text-blue-900 mr-3"
                    >
                      View
                    </button>
                    <button
                      @click="checkPlagiarismForSubmission(submission.id)"
                      class="text-purple-600 hover:text-purple-900 mr-3"
                    >
                      Plagiarism
                    </button>
                    <button
                      @click="deleteSubmission(submission.id)"
                      class="text-red-600 hover:text-red-900"
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- No Submissions -->
        <div v-else-if="!isLoading && filters.assignmentId" class="bg-white rounded-lg shadow-md p-8 text-center">
          <div class="text-gray-400 text-xl mb-2">📝</div>
          <div class="text-gray-600">No submissions found</div>
        </div>

        <!-- Submission Detail Modal -->
        <div v-if="selectedSubmission" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
          <div class="bg-white rounded-lg max-w-4xl w-full max-h-[90vh] overflow-y-auto p-6">
            <div class="flex justify-between items-center mb-4">
              <h3 class="text-xl font-semibold">Submission Details</h3>
              <button @click="selectedSubmission = null" class="text-gray-500 hover:text-gray-700 text-2xl">&times;</button>
            </div>
            
            <!-- Submission Info -->
            <div class="grid grid-cols-2 gap-4 mb-4">
              <div>
                <div class="text-sm text-gray-600">Student ID</div>
                <div class="font-medium">{{ selectedSubmission.student_id }}</div>
              </div>
              <div>
                <div class="text-sm text-gray-600">Filename</div>
                <div class="font-medium">{{ selectedSubmission.filename }}</div>
              </div>
              <div>
                <div class="text-sm text-gray-600">File Size</div>
                <div class="font-medium">{{ formatFileSize(selectedSubmission.file_size) }}</div>
              </div>
              <div>
                <div class="text-sm text-gray-600">Submitted</div>
                <div class="font-medium">{{ formatDate(selectedSubmission.submitted_at) }}</div>
              </div>
            </div>

            <!-- Code Content -->
            <div class="mb-4">
              <div class="text-sm text-gray-600 mb-2">Code</div>
              <pre class="bg-gray-100 p-4 rounded-lg overflow-x-auto max-h-96 text-sm">{{ selectedSubmission.code_content }}</pre>
            </div>

            <!-- Analysis Result -->
            <div v-if="selectedSubmission.analysis_result" class="mb-4">
              <h4 class="font-semibold mb-2">Analysis Result</h4>
              
              <div v-if="selectedSubmission.analysis_result.errors?.length > 0" class="mb-3">
                <div class="text-sm text-red-600 font-medium mb-1">Errors:</div>
                <ul class="list-disc list-inside bg-red-50 p-3 rounded text-sm">
                  <li v-for="(error, index) in selectedSubmission.analysis_result.errors" :key="index" class="text-red-700">
                    {{ error }}
                  </li>
                </ul>
              </div>

              <div v-if="selectedSubmission.analysis_result.warnings?.length > 0" class="mb-3">
                <div class="text-sm text-yellow-600 font-medium mb-1">Warnings:</div>
                <ul class="list-disc list-inside bg-yellow-50 p-3 rounded text-sm">
                  <li v-for="(warning, index) in selectedSubmission.analysis_result.warnings" :key="index" class="text-yellow-700">
                    {{ warning }}
                  </li>
                </ul>
              </div>

              <div v-if="selectedSubmission.analysis_result.metrics" class="mb-3">
                <div class="text-sm font-medium mb-1">Metrics:</div>
                <div class="grid grid-cols-3 gap-2">
                  <div v-for="(value, key) in selectedSubmission.analysis_result.metrics" :key="key" class="bg-gray-50 p-2 rounded text-sm">
                    <div class="text-xs text-gray-600">{{ formatMetricName(key) }}</div>
                    <div class="font-semibold">{{ value }}</div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Error Message -->
        <div v-if="errorMessage" class="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mt-4">
          {{ errorMessage }}
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import axios from 'axios';

export default {
  name: 'SubmissionsList',
  data() {
    return {
      filters: {
        assignmentId: '',
        studentId: ''
      },
      submissions: [],
      selectedSubmission: null,
      isLoading: false,
      errorMessage: ''
    };
  },
  methods: {
    async loadSubmissions() {
      if (!this.filters.assignmentId) return;

      this.isLoading = true;
      this.errorMessage = '';
      this.submissions = [];

      try {
        const url = this.filters.studentId
          ? `http://localhost:5015/api/submissions/student/${this.filters.studentId}?assignment_id=${this.filters.assignmentId}`
          : `http://localhost:5015/api/submissions/list/${this.filters.assignmentId}`;

        const response = await axios.get(url);
        this.submissions = response.data.submissions || [];
      } catch (error) {
        this.errorMessage = error.response?.data?.error || 'Failed to load submissions';
        console.error('Load error:', error);
      } finally {
        this.isLoading = false;
      }
    },

    viewSubmission(submission) {
      this.selectedSubmission = submission;
    },

    async checkPlagiarismForSubmission(submissionId) {
      try {
        const response = await axios.get(
          `http://localhost:5015/api/analysis/report/${submissionId}`
        );
        
        if (response.data.reports_found > 0) {
          alert(`Found ${response.data.reports_found} plagiarism report(s) for this submission`);
        } else {
          alert('No plagiarism detected for this submission');
        }
      } catch (error) {
        this.errorMessage = 'Failed to check plagiarism';
        console.error('Plagiarism check error:', error);
      }
    },

    async deleteSubmission(submissionId) {
      if (!confirm('Are you sure you want to delete this submission?')) return;

      try {
        await axios.delete(`http://localhost:5015/api/submissions/${submissionId}`);
        this.submissions = this.submissions.filter(s => s.id !== submissionId);
      } catch (error) {
        this.errorMessage = 'Failed to delete submission';
        console.error('Delete error:', error);
      }
    },

    formatDate(dateString) {
      return new Date(dateString).toLocaleString();
    },

    formatFileSize(bytes) {
      if (bytes < 1024) return bytes + ' B';
      if (bytes < 1048576) return (bytes / 1024).toFixed(2) + ' KB';
      return (bytes / 1048576).toFixed(2) + ' MB';
    },

    formatMetricName(key) {
      return key.split('_').map(word => 
        word.charAt(0).toUpperCase() + word.slice(1)
      ).join(' ');
    }
  }
};
</script>

<style scoped>
.submissions-list-page {
  min-height: 100vh;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

pre {
  font-family: 'Courier New', monospace;
}
</style>
