from flask import Blueprint, request
from controllers.analysis_controller import AnalysisController

analysis_bp = Blueprint('analysis', __name__)

@analysis_bp.route('/validate/<submission_id>', methods=['POST'])
def validate_code(submission_id):
    """Validate a code submission"""
    return AnalysisController.validate_code(submission_id)

@analysis_bp.route('/plagiarism/<assignment_id>', methods=['POST'])
def detect_plagiarism(assignment_id):
    """Detect plagiarism for an assignment"""
    return AnalysisController.detect_plagiarism(assignment_id)

@analysis_bp.route('/report/<submission_id>', methods=['GET'])
def get_plagiarism_report(submission_id):
    """Get plagiarism report for a submission"""
    return AnalysisController.get_plagiarism_report(submission_id)

@analysis_bp.route('/reports/<assignment_id>', methods=['GET'])
def get_assignment_plagiarism_reports(assignment_id):
    """Get all plagiarism reports for an assignment"""
    return AnalysisController.get_assignment_plagiarism_reports(assignment_id)

@analysis_bp.route('/compare', methods=['POST'])
def compare_submissions():
    """Compare two submissions"""
    data = request.json
    submission_a_id = data.get('submission_a_id')
    submission_b_id = data.get('submission_b_id')
    
    if not submission_a_id or not submission_b_id:
        return {"error": "Both submission_a_id and submission_b_id are required"}, 400
    
    return AnalysisController.compare_two_submissions(submission_a_id, submission_b_id)
