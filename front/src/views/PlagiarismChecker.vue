<template>
  <div class="plagiarism-checker-page">
    <div class="container mx-auto px-4 py-8">
      <div class="max-w-6xl mx-auto">
        <!-- Header -->
        <div class="mb-8">
          <h1 class="text-3xl font-bold text-gray-800 mb-2">Plagiarism Checker</h1>
          <p class="text-gray-600">Detect code plagiarism across all submissions for an assignment</p>
        </div>

        <!-- Check Form -->
        <div class="bg-white rounded-lg shadow-md p-6 mb-6">
          <h2 class="text-xl font-semibold mb-4">Run Plagiarism Detection</h2>
          
          <div class="flex gap-4">
            <input
              v-model="assignmentId"
              type="text"
              placeholder="Enter Assignment ID"
              class="flex-1 px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            />
            <button
              @click="checkPlagiarism"
              :disabled="!assignmentId || isChecking"
              class="bg-blue-500 text-white px-6 py-2 rounded-lg hover:bg-blue-600 transition-colors disabled:bg-gray-400 disabled:cursor-not-allowed"
            >
              <span v-if="!isChecking">Check Plagiarism</span>
              <span v-else>Checking...</span>
            </button>
          </div>
        </div>

        <!-- Results Summary -->
        <div v-if="results" class="bg-white rounded-lg shadow-md p-6 mb-6">
          <h2 class="text-xl font-semibold mb-4">Analysis Summary</h2>
          
          <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
            <div class="bg-blue-50 p-4 rounded-lg">
              <div class="text-sm text-blue-600">Submissions Analyzed</div>
              <div class="text-2xl font-bold text-blue-700">{{ results.submissions_analyzed }}</div>
            </div>
            <div class="bg-red-50 p-4 rounded-lg">
              <div class="text-sm text-red-600">Plagiarism Cases Found</div>
              <div class="text-2xl font-bold text-red-700">{{ results.plagiarism_cases_found }}</div>
            </div>
            <div class="bg-green-50 p-4 rounded-lg">
              <div class="text-sm text-green-600">Threshold</div>
              <div class="text-2xl font-bold text-green-700">{{ (results.threshold * 100).toFixed(0) }}%</div>
            </div>
          </div>

          <!-- No Plagiarism Found -->
          <div v-if="results.plagiarism_cases_found === 0" class="bg-green-100 border border-green-400 text-green-700 px-4 py-3 rounded">
            ✓ No plagiarism detected. All submissions appear to be original work.
          </div>
        </div>

        <!-- Plagiarism Cases -->
        <div v-if="results && results.plagiarism_cases && results.plagiarism_cases.length > 0">
          <h2 class="text-xl font-semibold mb-4 text-gray-800">Plagiarism Cases</h2>
          
          <div v-for="(caseItem, index) in results.plagiarism_cases" :key="index" class="bg-white rounded-lg shadow-md p-6 mb-4">
            <div class="flex items-center justify-between mb-4">
              <h3 class="text-lg font-semibold text-red-600">Case #{{ index + 1 }}</h3>
              <span class="px-3 py-1 bg-red-100 text-red-700 rounded-full text-sm font-medium">
                {{ (caseItem.similarity_score * 100).toFixed(2) }}% Similar
              </span>
            </div>

            <!-- Students Involved -->
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
              <div class="bg-gray-50 p-4 rounded">
                <div class="text-sm text-gray-600 mb-1">Student A</div>
                <div class="font-medium">{{ caseItem.student_a_id }}</div>
                <div class="text-xs text-gray-500">Submission: {{ caseItem.submission_a_id.substring(0, 8) }}...</div>
              </div>
              <div class="bg-gray-50 p-4 rounded">
                <div class="text-sm text-gray-600 mb-1">Student B</div>
                <div class="font-medium">{{ caseItem.student_b_id }}</div>
                <div class="text-xs text-gray-500">Submission: {{ caseItem.submission_b_id.substring(0, 8) }}...</div>
              </div>
            </div>

            <!-- Similarity Breakdown -->
            <div class="grid grid-cols-3 gap-4 mb-4">
              <div class="text-center">
                <div class="text-sm text-gray-600">Text Similarity</div>
                <div class="text-lg font-semibold" :class="getSimilarityColor(caseItem.text_similarity)">
                  {{ (caseItem.text_similarity * 100).toFixed(1) }}%
                </div>
              </div>
              <div class="text-center">
                <div class="text-sm text-gray-600">Structure Similarity</div>
                <div class="text-lg font-semibold" :class="getSimilarityColor(caseItem.structure_similarity)">
                  {{ (caseItem.structure_similarity * 100).toFixed(1) }}%
                </div>
              </div>
              <div class="text-center">
                <div class="text-sm text-gray-600">Overall Score</div>
                <div class="text-lg font-semibold" :class="getSimilarityColor(caseItem.similarity_score)">
                  {{ (caseItem.similarity_score * 100).toFixed(1) }}%
                </div>
              </div>
            </div>

            <!-- Details -->
            <div class="bg-yellow-50 border border-yellow-300 rounded p-3">
              <div class="text-sm font-medium text-yellow-800">{{ caseItem.details }}</div>
            </div>

            <!-- Actions -->
            <div class="mt-4 flex gap-2">
              <button
                @click="compareSubmissions(caseItem.submission_a_id, caseItem.submission_b_id)"
                class="text-sm bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 transition-colors"
              >
                View Detailed Comparison
              </button>
              <button
                @click="viewSubmission(caseItem.submission_a_id)"
                class="text-sm bg-gray-500 text-white px-4 py-2 rounded hover:bg-gray-600 transition-colors"
              >
                View Submission A
              </button>
              <button
                @click="viewSubmission(caseItem.submission_b_id)"
                class="text-sm bg-gray-500 text-white px-4 py-2 rounded hover:bg-gray-600 transition-colors"
              >
                View Submission B
              </button>
            </div>
          </div>
        </div>

        <!-- Error Message -->
        <div v-if="errorMessage" class="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded">
          {{ errorMessage }}
        </div>

        <!-- Comparison Modal -->
        <div v-if="comparisonData" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
          <div class="bg-white rounded-lg max-w-4xl w-full max-h-[90vh] overflow-y-auto p-6">
            <div class="flex justify-between items-center mb-4">
              <h3 class="text-xl font-semibold">Detailed Comparison</h3>
              <button @click="comparisonData = null" class="text-gray-500 hover:text-gray-700 text-2xl">&times;</button>
            </div>
            
            <div class="mb-4">
              <div class="grid grid-cols-2 gap-4 mb-4">
                <div class="bg-blue-50 p-3 rounded">
                  <div class="text-sm text-blue-600">Submission A</div>
                  <div class="font-medium">{{ comparisonData.submission_a.student_id }}</div>
                  <div class="text-xs text-gray-500">{{ comparisonData.submission_a.filename }}</div>
                </div>
                <div class="bg-blue-50 p-3 rounded">
                  <div class="text-sm text-blue-600">Submission B</div>
                  <div class="font-medium">{{ comparisonData.submission_b.student_id }}</div>
                  <div class="text-xs text-gray-500">{{ comparisonData.submission_b.filename }}</div>
                </div>
              </div>

              <div class="bg-gray-100 p-4 rounded">
                <div class="grid grid-cols-2 gap-4">
                  <div>
                    <div class="text-sm text-gray-600">Overall Similarity</div>
                    <div class="text-2xl font-bold" :class="getSimilarityColor(comparisonData.similarity.overall_similarity)">
                      {{ (comparisonData.similarity.overall_similarity * 100).toFixed(2) }}%
                    </div>
                  </div>
                  <div>
                    <div class="text-sm text-gray-600">Matching Blocks</div>
                    <div class="text-2xl font-bold">{{ comparisonData.matching_blocks }}</div>
                  </div>
                </div>
                <div class="mt-2 text-sm">{{ comparisonData.similarity.details }}</div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import axios from 'axios';

export default {
  name: 'PlagiarismChecker',
  data() {
    return {
      assignmentId: '',
      isChecking: false,
      results: null,
      errorMessage: '',
      comparisonData: null
    };
  },
  methods: {
    async checkPlagiarism() {
      if (!this.assignmentId) return;

      this.isChecking = true;
      this.errorMessage = '';
      this.results = null;

      try {
        const response = await axios.post(
          `http://localhost:5015/api/analysis/plagiarism/${this.assignmentId}`
        );

        this.results = response.data;
      } catch (error) {
        this.errorMessage = error.response?.data?.error || 'Failed to check plagiarism';
        console.error('Plagiarism check error:', error);
      } finally {
        this.isChecking = false;
      }
    },

    async compareSubmissions(submissionAId, submissionBId) {
      try {
        const response = await axios.post(
          'http://localhost:5015/api/analysis/compare',
          {
            submission_a_id: submissionAId,
            submission_b_id: submissionBId
          }
        );

        this.comparisonData = response.data;
      } catch (error) {
        this.errorMessage = 'Failed to compare submissions';
        console.error('Comparison error:', error);
      }
    },

    viewSubmission(submissionId) {
      // Navigate to submission detail page or open in new window
      window.open(`/submission/${submissionId}`, '_blank');
    },

    getSimilarityColor(score) {
      if (score >= 0.9) return 'text-red-700';
      if (score >= 0.75) return 'text-red-600';
      if (score >= 0.5) return 'text-yellow-600';
      if (score >= 0.3) return 'text-blue-600';
      return 'text-green-600';
    }
  }
};
</script>

<style scoped>
.plagiarism-checker-page {
  min-height: 100vh;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}
</style>
