from flask import Blueprint, request
from controllers.submission_controller import SubmissionController

submission_bp = Blueprint('submissions', __name__)

@submission_bp.route('/upload', methods=['POST'])
def upload_submission():
    """Upload a new code submission"""
    return SubmissionController.upload_submission()

@submission_bp.route('/list/<assignment_id>', methods=['GET'])
def get_submissions_by_assignment(assignment_id):
    """Get all submissions for an assignment"""
    return SubmissionController.get_submissions_by_assignment(assignment_id)

@submission_bp.route('/<submission_id>', methods=['GET'])
def get_submission_by_id(submission_id):
    """Get a specific submission"""
    return SubmissionController.get_submission_by_id(submission_id)

@submission_bp.route('/student/<student_id>', methods=['GET'])
def get_student_submissions(student_id):
    """Get all submissions for a student"""
    assignment_id = request.args.get('assignment_id')
    return SubmissionController.get_student_submissions(student_id, assignment_id)

@submission_bp.route('/<submission_id>', methods=['DELETE'])
def delete_submission(submission_id):
    """Delete a submission"""
    return SubmissionController.delete_submission(submission_id)
