from flask import jsonify
from datetime import datetime
from utils.supabase import get_supabase_client
from utils.code_analyzer import CodeAnalyzer
from utils.plagiarism_detector import PlagiarismDetector
import os
import traceback

class AnalysisController:
    """Controller for code analysis and plagiarism detection"""
    
    @staticmethod
    def validate_code(submission_id):
        """Validate a code submission"""
        try:
            supabase = get_supabase_client()
            
            # Get submission
            result = supabase.table('code_submissions').select('*').eq('id', submission_id).execute()
            
            if not result.data:
                return jsonify({"error": "Submission not found"}), 404
            
            submission = result.data[0]
            
            # Analyze code
            analyzer = CodeAnalyzer(submission['code_content'], submission['file_extension'])
            analysis = analyzer.analyze()
            
            # Save or update analysis
            existing_analysis = supabase.table('code_analysis').select('*').eq('submission_id', submission_id).execute()
            
            analysis_data = {
                "submission_id": submission_id,
                "is_valid": analysis['is_valid'],
                "language": analysis['language'],
                "errors": analysis['errors'],
                "warnings": analysis['warnings'],
                "metrics": analysis['metrics'],
                "suggestions": analysis['suggestions'],
                "analyzed_at": datetime.utcnow().isoformat()
            }
            
            if existing_analysis.data:
                # Update
                supabase.table('code_analysis').update(analysis_data).eq('submission_id', submission_id).execute()
            else:
                # Insert
                supabase.table('code_analysis').insert(analysis_data).execute()
            
            # Update submission
            supabase.table('code_submissions').update({
                "is_analyzed": True,
                "analysis_result": analysis
            }).eq('id', submission_id).execute()
            
            return jsonify({
                "submission_id": submission_id,
                "analysis": analysis
            }), 200
            
        except Exception as e:
            print(f"Error validating code: {str(e)}")
            print(traceback.format_exc())
            return jsonify({"error": f"Internal server error: {str(e)}"}), 500
    
    @staticmethod
    def detect_plagiarism(assignment_id):
        """Detect plagiarism among all submissions for an assignment"""
        try:
            supabase = get_supabase_client()
            
            # Get all submissions for the assignment
            result = supabase.table('code_submissions').select('*').eq('assignment_id', assignment_id).execute()
            
            if not result.data or len(result.data) < 2:
                return jsonify({
                    "assignment_id": assignment_id,
                    "message": "Not enough submissions to compare",
                    "plagiarism_cases": []
                }), 200
            
            submissions = result.data
            
            # Prepare data for comparison
            comparison_data = []
            for sub in submissions:
                comparison_data.append({
                    'id': sub['id'],
                    'student_id': sub['student_id'],
                    'code': sub['code_content'],
                    'language': sub['file_extension']
                })
            
            # Detect plagiarism
            threshold = float(os.getenv('PLAGIARISM_THRESHOLD', 0.75))
            detector = PlagiarismDetector(threshold=threshold)
            plagiarism_cases = detector.compare_submissions(comparison_data)
            
            # Save plagiarism reports
            for case in plagiarism_cases:
                report_data = {
                    "assignment_id": assignment_id,
                    "submission_a_id": case['submission_a_id'],
                    "submission_b_id": case['submission_b_id'],
                    "student_a_id": case['student_a_id'],
                    "student_b_id": case['student_b_id'],
                    "similarity_score": case['similarity_score'],
                    "text_similarity": case['text_similarity'],
                    "structure_similarity": case['structure_similarity'],
                    "is_plagiarism": case['is_plagiarism'],
                    "details": case['details'],
                    "created_at": datetime.utcnow().isoformat()
                }
                
                # Check if report already exists
                existing = supabase.table('plagiarism_reports').select('*').eq('submission_a_id', case['submission_a_id']).eq('submission_b_id', case['submission_b_id']).execute()
                
                if existing.data:
                    # Update
                    supabase.table('plagiarism_reports').update(report_data).eq('id', existing.data[0]['id']).execute()
                else:
                    # Insert
                    supabase.table('plagiarism_reports').insert(report_data).execute()
            
            return jsonify({
                "assignment_id": assignment_id,
                "submissions_analyzed": len(submissions),
                "plagiarism_cases_found": len(plagiarism_cases),
                "threshold": threshold,
                "plagiarism_cases": plagiarism_cases
            }), 200
            
        except Exception as e:
            print(f"Error detecting plagiarism: {str(e)}")
            print(traceback.format_exc())
            return jsonify({"error": f"Internal server error: {str(e)}"}), 500
    
    @staticmethod
    def get_plagiarism_report(submission_id):
        """Get plagiarism report for a specific submission"""
        try:
            supabase = get_supabase_client()
            
            # Get reports where this submission is involved
            result_a = supabase.table('plagiarism_reports').select('*').eq('submission_a_id', submission_id).execute()
            result_b = supabase.table('plagiarism_reports').select('*').eq('submission_b_id', submission_id).execute()
            
            reports = result_a.data + result_b.data
            
            return jsonify({
                "submission_id": submission_id,
                "reports_found": len(reports),
                "reports": reports
            }), 200
            
        except Exception as e:
            print(f"Error fetching plagiarism report: {str(e)}")
            return jsonify({"error": f"Internal server error: {str(e)}"}), 500
    
    @staticmethod
    def get_assignment_plagiarism_reports(assignment_id):
        """Get all plagiarism reports for an assignment"""
        try:
            supabase = get_supabase_client()
            
            result = supabase.table('plagiarism_reports').select('*').eq('assignment_id', assignment_id).execute()
            
            return jsonify({
                "assignment_id": assignment_id,
                "reports_found": len(result.data),
                "reports": result.data
            }), 200
            
        except Exception as e:
            print(f"Error fetching assignment plagiarism reports: {str(e)}")
            return jsonify({"error": f"Internal server error: {str(e)}"}), 500
    
    @staticmethod
    def compare_two_submissions(submission_a_id, submission_b_id):
        """Compare two specific submissions"""
        try:
            supabase = get_supabase_client()
            
            # Get both submissions
            result_a = supabase.table('code_submissions').select('*').eq('id', submission_a_id).execute()
            result_b = supabase.table('code_submissions').select('*').eq('id', submission_b_id).execute()
            
            if not result_a.data or not result_b.data:
                return jsonify({"error": "One or both submissions not found"}), 404
            
            sub_a = result_a.data[0]
            sub_b = result_b.data[0]
            
            # Compare
            detector = PlagiarismDetector()
            similarity = detector.calculate_similarity(
                sub_a['code_content'],
                sub_b['code_content'],
                sub_a['file_extension']
            )
            
            # Get matching blocks
            matching_blocks = detector.get_matching_blocks(
                sub_a['code_content'],
                sub_b['code_content']
            )
            
            return jsonify({
                "submission_a": {
                    "id": sub_a['id'],
                    "student_id": sub_a['student_id'],
                    "filename": sub_a['filename']
                },
                "submission_b": {
                    "id": sub_b['id'],
                    "student_id": sub_b['student_id'],
                    "filename": sub_b['filename']
                },
                "similarity": similarity,
                "matching_blocks": len(matching_blocks),
                "is_plagiarism": similarity['overall_similarity'] >= 0.75
            }), 200
            
        except Exception as e:
            print(f"Error comparing submissions: {str(e)}")
            print(traceback.format_exc())
            return jsonify({"error": f"Internal server error: {str(e)}"}), 500
