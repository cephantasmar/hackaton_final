from flask import Flask, request, jsonify
from flask_cors import CORS
import os
from routes.submission_routes import submission_bp
from routes.analysis_routes import analysis_bp
from utils.supabase import get_supabase_client

app = Flask(__name__)
CORS(app)

# Register blueprints
app.register_blueprint(submission_bp, url_prefix='/api/submissions')
app.register_blueprint(analysis_bp, url_prefix='/api/analysis')

@app.route('/health', methods=['GET'])
def health_check():
    return jsonify({"status": "healthy", "service": "code-analysis"}), 200

@app.route('/', methods=['GET'])
def root():
    return jsonify({
        "service": "Code Analysis & Plagiarism Detection",
        "version": "1.0.0",
        "endpoints": [
            "/api/submissions/upload",
            "/api/submissions/list/<assignment_id>",
            "/api/analysis/validate/<submission_id>",
            "/api/analysis/plagiarism/<assignment_id>",
            "/api/analysis/report/<submission_id>"
        ]
    }), 200

if __name__ == '__main__':
    print("🚀 Starting Code Analysis Service...")
    print(f"📊 Environment: {os.getenv('FLASK_ENV', 'production')}")
    app.run(host='0.0.0.0', port=5000, debug=True)
