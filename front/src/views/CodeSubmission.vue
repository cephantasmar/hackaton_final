<template>
  <div class="code-submission-page">
    <div class="container mx-auto px-4 py-8">
      <div class="max-w-4xl mx-auto">
        <!-- Header -->
        <div class="mb-8">
          <h1 class="text-3xl font-bold text-gray-800 mb-2">Submit Code Assignment</h1>
          <p class="text-gray-600">Upload your code file for automatic validation and plagiarism detection</p>
        </div>

        <!-- Upload Form -->
        <div class="bg-white rounded-lg shadow-md p-6 mb-6">
          <h2 class="text-xl font-semibold mb-4">Upload Code</h2>
          
          <form @submit.prevent="submitCode">
            <!-- Assignment ID -->
            <div class="mb-4">
              <label class="block text-gray-700 font-medium mb-2">Assignment ID</label>
              <input
                v-model="formData.assignment_id"
                type="text"
                required
                class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                placeholder="Enter assignment ID"
              />
            </div>

            <!-- Student ID -->
            <div class="mb-4">
              <label class="block text-gray-700 font-medium mb-2">Student ID</label>
              <input
                v-model="formData.student_id"
                type="text"
                required
                class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                placeholder="Enter your student ID"
              />
            </div>

            <!-- File Upload -->
            <div class="mb-4">
              <label class="block text-gray-700 font-medium mb-2">Code File</label>
              <div class="border-2 border-dashed border-gray-300 rounded-lg p-6 text-center hover:border-blue-500 transition-colors">
                <input
                  type="file"
                  ref="fileInput"
                  @change="handleFileChange"
                  accept=".py,.js,.java,.cpp,.c,.cs,.ts,.jsx,.tsx,.html,.css"
                  class="hidden"
                />
                <button
                  type="button"
                  @click="$refs.fileInput.click()"
                  class="bg-blue-500 text-white px-6 py-2 rounded-lg hover:bg-blue-600 transition-colors"
                >
                  Choose File
                </button>
                <p class="mt-2 text-sm text-gray-500">
                  Accepted: .py, .js, .java, .cpp, .c, .cs, .ts, .jsx, .tsx, .html, .css
                </p>
                <p v-if="selectedFile" class="mt-2 text-green-600 font-medium">
                  Selected: {{ selectedFile.name }} ({{ formatFileSize(selectedFile.size) }})
                </p>
              </div>
            </div>

            <!-- Code Preview (if file selected) -->
            <div v-if="codePreview" class="mb-4">
              <label class="block text-gray-700 font-medium mb-2">Code Preview</label>
              <pre class="bg-gray-100 p-4 rounded-lg overflow-x-auto max-h-64 text-sm">{{ codePreview }}</pre>
            </div>

            <!-- Submit Button -->
            <button
              type="submit"
              :disabled="!selectedFile || isSubmitting"
              class="w-full bg-green-500 text-white py-3 rounded-lg font-semibold hover:bg-green-600 transition-colors disabled:bg-gray-400 disabled:cursor-not-allowed"
            >
              <span v-if="!isSubmitting">Submit Code</span>
              <span v-else>Submitting...</span>
            </button>
          </form>
        </div>

        <!-- Analysis Result -->
        <div v-if="analysisResult" class="bg-white rounded-lg shadow-md p-6 mb-6">
          <h2 class="text-xl font-semibold mb-4">Analysis Result</h2>
          
          <!-- Validation Status -->
          <div class="mb-4">
            <div v-if="analysisResult.is_valid" class="bg-green-100 border border-green-400 text-green-700 px-4 py-3 rounded">
              ✓ Code is valid and passes syntax check
            </div>
            <div v-else class="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded">
              ✗ Code has errors and needs fixing
            </div>
          </div>

          <!-- Errors -->
          <div v-if="analysisResult.errors && analysisResult.errors.length > 0" class="mb-4">
            <h3 class="font-semibold text-red-600 mb-2">Errors:</h3>
            <ul class="list-disc list-inside bg-red-50 p-4 rounded">
              <li v-for="(error, index) in analysisResult.errors" :key="index" class="text-red-700">
                {{ error }}
              </li>
            </ul>
          </div>

          <!-- Warnings -->
          <div v-if="analysisResult.warnings && analysisResult.warnings.length > 0" class="mb-4">
            <h3 class="font-semibold text-yellow-600 mb-2">Warnings:</h3>
            <ul class="list-disc list-inside bg-yellow-50 p-4 rounded">
              <li v-for="(warning, index) in analysisResult.warnings" :key="index" class="text-yellow-700">
                {{ warning }}
              </li>
            </ul>
          </div>

          <!-- Metrics -->
          <div v-if="analysisResult.metrics" class="mb-4">
            <h3 class="font-semibold mb-2">Code Metrics:</h3>
            <div class="grid grid-cols-2 md:grid-cols-3 gap-4">
              <div v-for="(value, key) in analysisResult.metrics" :key="key" class="bg-gray-50 p-3 rounded">
                <div class="text-sm text-gray-600">{{ formatMetricName(key) }}</div>
                <div class="text-lg font-semibold">{{ value }}</div>
              </div>
            </div>
          </div>

          <!-- Suggestions -->
          <div v-if="analysisResult.suggestions && analysisResult.suggestions.length > 0" class="mb-4">
            <h3 class="font-semibold text-blue-600 mb-2">Suggestions:</h3>
            <ul class="list-disc list-inside bg-blue-50 p-4 rounded">
              <li v-for="(suggestion, index) in analysisResult.suggestions" :key="index" class="text-blue-700">
                {{ suggestion }}
              </li>
            </ul>
          </div>
        </div>

        <!-- Success/Error Messages -->
        <div v-if="successMessage" class="bg-green-100 border border-green-400 text-green-700 px-4 py-3 rounded mb-4">
          {{ successMessage }}
        </div>
        <div v-if="errorMessage" class="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
          {{ errorMessage }}
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import axios from 'axios';

export default {
  name: 'CodeSubmission',
  data() {
    return {
      formData: {
        assignment_id: '',
        student_id: ''
      },
      selectedFile: null,
      codePreview: '',
      isSubmitting: false,
      analysisResult: null,
      successMessage: '',
      errorMessage: ''
    };
  },
  methods: {
    handleFileChange(event) {
      const file = event.target.files[0];
      if (file) {
        this.selectedFile = file;
        
        // Read file for preview
        const reader = new FileReader();
        reader.onload = (e) => {
          this.codePreview = e.target.result.substring(0, 1000); // First 1000 chars
        };
        reader.readAsText(file);
      }
    },
    
    async submitCode() {
      if (!this.selectedFile) {
        this.errorMessage = 'Please select a file';
        return;
      }

      this.isSubmitting = true;
      this.errorMessage = '';
      this.successMessage = '';
      this.analysisResult = null;

      try {
        const formData = new FormData();
        formData.append('file', this.selectedFile);
        formData.append('assignment_id', this.formData.assignment_id);
        formData.append('student_id', this.formData.student_id);

        const response = await axios.post(
          'http://localhost:5015/api/submissions/upload',
          formData,
          {
            headers: {
              'Content-Type': 'multipart/form-data'
            }
          }
        );

        this.successMessage = 'Code submitted and analyzed successfully!';
        this.analysisResult = response.data.analysis;
        
        // Reset form
        this.selectedFile = null;
        this.codePreview = '';
        this.$refs.fileInput.value = '';
        
      } catch (error) {
        this.errorMessage = error.response?.data?.error || 'Failed to submit code';
        console.error('Submission error:', error);
      } finally {
        this.isSubmitting = false;
      }
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
.code-submission-page {
  min-height: 100vh;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

pre {
  font-family: 'Courier New', monospace;
}
</style>
