from datetime import datetime

class Submission:
    """Model for code submission"""
    
    def __init__(self, data=None):
        if data:
            self.id = data.get('id')
            self.assignment_id = data.get('assignment_id')
            self.student_id = data.get('student_id')
            self.filename = data.get('filename')
            self.file_extension = data.get('file_extension')
            self.code_content = data.get('code_content')
            self.file_size = data.get('file_size')
            self.encoding = data.get('encoding')
            self.submitted_at = data.get('submitted_at')
            self.is_analyzed = data.get('is_analyzed', False)
            self.analysis_result = data.get('analysis_result')
    
    def to_dict(self):
        """Convert to dictionary"""
        return {
            'id': self.id,
            'assignment_id': self.assignment_id,
            'student_id': self.student_id,
            'filename': self.filename,
            'file_extension': self.file_extension,
            'code_content': self.code_content,
            'file_size': self.file_size,
            'encoding': self.encoding,
            'submitted_at': self.submitted_at,
            'is_analyzed': self.is_analyzed,
            'analysis_result': self.analysis_result
        }

class AnalysisResult:
    """Model for code analysis result"""
    
    def __init__(self, data=None):
        if data:
            self.id = data.get('id')
            self.submission_id = data.get('submission_id')
            self.is_valid = data.get('is_valid')
            self.language = data.get('language')
            self.errors = data.get('errors', [])
            self.warnings = data.get('warnings', [])
            self.metrics = data.get('metrics', {})
            self.suggestions = data.get('suggestions', [])
            self.analyzed_at = data.get('analyzed_at')
    
    def to_dict(self):
        """Convert to dictionary"""
        return {
            'id': self.id,
            'submission_id': self.submission_id,
            'is_valid': self.is_valid,
            'language': self.language,
            'errors': self.errors,
            'warnings': self.warnings,
            'metrics': self.metrics,
            'suggestions': self.suggestions,
            'analyzed_at': self.analyzed_at
        }

class PlagiarismReport:
    """Model for plagiarism detection report"""
    
    def __init__(self, data=None):
        if data:
            self.id = data.get('id')
            self.assignment_id = data.get('assignment_id')
            self.submission_a_id = data.get('submission_a_id')
            self.submission_b_id = data.get('submission_b_id')
            self.student_a_id = data.get('student_a_id')
            self.student_b_id = data.get('student_b_id')
            self.similarity_score = data.get('similarity_score')
            self.text_similarity = data.get('text_similarity')
            self.structure_similarity = data.get('structure_similarity')
            self.is_plagiarism = data.get('is_plagiarism')
            self.details = data.get('details')
            self.created_at = data.get('created_at')
    
    def to_dict(self):
        """Convert to dictionary"""
        return {
            'id': self.id,
            'assignment_id': self.assignment_id,
            'submission_a_id': self.submission_a_id,
            'submission_b_id': self.submission_b_id,
            'student_a_id': self.student_a_id,
            'student_b_id': self.student_b_id,
            'similarity_score': self.similarity_score,
            'text_similarity': self.text_similarity,
            'structure_similarity': self.structure_similarity,
            'is_plagiarism': self.is_plagiarism,
            'details': self.details,
            'created_at': self.created_at
        }
