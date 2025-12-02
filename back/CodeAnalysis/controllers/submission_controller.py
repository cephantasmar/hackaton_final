from flask import request, jsonify
from werkzeug.utils import secure_filename
from datetime import datetime
from utils.supabase import get_supabase_client
from utils.file_handler import allowed_file, get_file_extension, read_file_content, validate_file_size
from utils.code_analyzer import CodeAnalyzer
import traceback

class SubmissionController:
    """Controller for handling code submissions"""
    
    @staticmethod
    def upload_submission():
        """Upload a new code submission"""
        try:
            # Validate request
            if 'file' not in request.files:
                return jsonify({"error": "No file provided"}), 400
            
            file = request.files['file']
            
            if file.filename == '':
                return jsonify({"error": "No file selected"}), 400
            
            # Get form data
            assignment_id = request.form.get('assignment_id')
            student_id = request.form.get('student_id')
            
            if not assignment_id or not student_id:
                return jsonify({"error": "assignment_id and student_id are required"}), 400
            
            # Validate file
            if not allowed_file(file.filename):
                return jsonify({
                    "error": "File type not allowed",
                    "allowed_extensions": ["py", "js", "java", "cpp", "c", "cs", "ts", "jsx", "tsx", "html", "css"]
                }), 400
            
            # Validate file size
            try:
                file_size = validate_file_size(file)
            except ValueError as e:
                return jsonify({"error": str(e)}), 400
            
            # Read file content
            try:
                content, encoding = read_file_content(file)
            except ValueError as e:
                return jsonify({"error": str(e)}), 400
            
            filename = secure_filename(file.filename)
            file_extension = get_file_extension(filename)
            
            # Save to database
            supabase = get_supabase_client()
            
            submission_data = {
                "assignment_id": assignment_id,
                "student_id": student_id,
                "filename": filename,
                "file_extension": file_extension,
                "code_content": content,
                "file_size": file_size,
                "encoding": encoding,
                "submitted_at": datetime.utcnow().isoformat(),
                "is_analyzed": False
            }
            
            result = supabase.table('code_submissions').insert(submission_data).execute()
            
            if not result.data:
                return jsonify({"error": "Failed to save submission"}), 500
            
            submission = result.data[0]
            
            # Analyze code immediately
            analyzer = CodeAnalyzer(content, file_extension)
            analysis = analyzer.analyze()
            
            # Save analysis result
            analysis_data = {
                "submission_id": submission['id'],
                "is_valid": analysis['is_valid'],
                "language": analysis['language'],
                "errors": analysis['errors'],
                "warnings": analysis['warnings'],
                "metrics": analysis['metrics'],
                "suggestions": analysis['suggestions'],
                "analyzed_at": datetime.utcnow().isoformat()
            }
            
            analysis_result = supabase.table('code_analysis').insert(analysis_data).execute()
            
            # Update submission as analyzed
            supabase.table('code_submissions').update({
                "is_analyzed": True,
                "analysis_result": analysis
            }).eq('id', submission['id']).execute()
            
            return jsonify({
                "message": "Submission uploaded and analyzed successfully",
                "submission": submission,
                "analysis": analysis
            }), 201
            
        except Exception as e:
            print(f"Error uploading submission: {str(e)}")
            print(traceback.format_exc())
            return jsonify({"error": f"Internal server error: {str(e)}"}), 500
    
    @staticmethod
    def get_submissions_by_assignment(assignment_id):
        """Get all submissions for an assignment"""
        try:
            supabase = get_supabase_client()
            
            result = supabase.table('code_submissions').select('*').eq('assignment_id', assignment_id).execute()
            
            return jsonify({
                "assignment_id": assignment_id,
                "submissions": result.data,
                "count": len(result.data)
            }), 200
            
        except Exception as e:
            print(f"Error fetching submissions: {str(e)}")
            return jsonify({"error": f"Internal server error: {str(e)}"}), 500
    
    @staticmethod
    def get_submission_by_id(submission_id):
        """Get a specific submission"""
        try:
            supabase = get_supabase_client()
            
            result = supabase.table('code_submissions').select('*').eq('id', submission_id).execute()
            
            if not result.data:
                return jsonify({"error": "Submission not found"}), 404
            
            submission = result.data[0]
            
            # Get analysis if exists
            analysis_result = supabase.table('code_analysis').select('*').eq('submission_id', submission_id).execute()
            
            if analysis_result.data:
                submission['analysis'] = analysis_result.data[0]
            
            return jsonify(submission), 200
            
        except Exception as e:
            print(f"Error fetching submission: {str(e)}")
            return jsonify({"error": f"Internal server error: {str(e)}"}), 500
    
    @staticmethod
    def get_student_submissions(student_id, assignment_id=None):
        """Get all submissions for a student"""
        try:
            supabase = get_supabase_client()
            
            query = supabase.table('code_submissions').select('*').eq('student_id', student_id)
            
            if assignment_id:
                query = query.eq('assignment_id', assignment_id)
            
            result = query.execute()
            
            return jsonify({
                "student_id": student_id,
                "submissions": result.data,
                "count": len(result.data)
            }), 200
            
        except Exception as e:
            print(f"Error fetching student submissions: {str(e)}")
            return jsonify({"error": f"Internal server error: {str(e)}"}), 500
    
    @staticmethod
    def delete_submission(submission_id):
        """Delete a submission"""
        try:
            supabase = get_supabase_client()
            
            # Delete analysis first
            supabase.table('code_analysis').delete().eq('submission_id', submission_id).execute()
            
            # Delete submission
            result = supabase.table('code_submissions').delete().eq('id', submission_id).execute()
            
            return jsonify({"message": "Submission deleted successfully"}), 200
            
        except Exception as e:
            print(f"Error deleting submission: {str(e)}")
            return jsonify({"error": f"Internal server error: {str(e)}"}), 500
