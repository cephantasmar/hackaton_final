import os
import chardet
from werkzeug.utils import secure_filename

ALLOWED_EXTENSIONS = os.getenv('ALLOWED_EXTENSIONS', 'py,js,java,cpp,c,cs,ts,jsx,tsx,html,css').split(',')
MAX_FILE_SIZE = int(os.getenv('MAX_FILE_SIZE', 10485760))  # 10MB default

def allowed_file(filename):
    """Check if file extension is allowed"""
    return '.' in filename and filename.rsplit('.', 1)[1].lower() in ALLOWED_EXTENSIONS

def get_file_extension(filename):
    """Get file extension"""
    return filename.rsplit('.', 1)[1].lower() if '.' in filename else ''

def detect_encoding(file_content):
    """Detect file encoding"""
    result = chardet.detect(file_content)
    return result['encoding'] or 'utf-8'

def read_file_content(file):
    """Read and decode file content"""
    try:
        file_bytes = file.read()
        encoding = detect_encoding(file_bytes)
        content = file_bytes.decode(encoding)
        return content, encoding
    except Exception as e:
        raise ValueError(f"Error reading file: {str(e)}")

def validate_file_size(file):
    """Validate file size"""
    file.seek(0, os.SEEK_END)
    file_size = file.tell()
    file.seek(0)
    
    if file_size > MAX_FILE_SIZE:
        raise ValueError(f"File size exceeds maximum allowed size of {MAX_FILE_SIZE} bytes")
    
    return file_size
