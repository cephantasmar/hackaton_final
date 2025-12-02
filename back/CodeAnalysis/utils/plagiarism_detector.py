import difflib
import re
import ast
from typing import List, Dict, Tuple

class PlagiarismDetector:
    """Detects code plagiarism using multiple algorithms"""
    
    def __init__(self, threshold=0.75):
        self.threshold = threshold
    
    def compare_submissions(self, submissions: List[Dict]) -> List[Dict]:
        """Compare all submissions and detect plagiarism"""
        results = []
        
        for i in range(len(submissions)):
            for j in range(i + 1, len(submissions)):
                submission_a = submissions[i]
                submission_b = submissions[j]
                
                similarity = self.calculate_similarity(
                    submission_a['code'],
                    submission_b['code'],
                    submission_a['language']
                )
                
                if similarity['overall_similarity'] >= self.threshold:
                    results.append({
                        "submission_a_id": submission_a['id'],
                        "submission_b_id": submission_b['id'],
                        "student_a_id": submission_a['student_id'],
                        "student_b_id": submission_b['student_id'],
                        "similarity_score": similarity['overall_similarity'],
                        "text_similarity": similarity['text_similarity'],
                        "structure_similarity": similarity.get('structure_similarity', 0),
                        "details": similarity['details'],
                        "is_plagiarism": True
                    })
        
        return results
    
    def calculate_similarity(self, code_a: str, code_b: str, language: str) -> Dict:
        """Calculate similarity between two code submissions"""
        # Text-based similarity
        text_similarity = self._text_similarity(code_a, code_b)
        
        # Structure-based similarity (for supported languages)
        structure_similarity = 0
        if language == 'py':
            structure_similarity = self._python_structure_similarity(code_a, code_b)
        elif language in ['js', 'jsx', 'ts', 'tsx', 'java', 'c', 'cpp', 'cs']:
            structure_similarity = self._token_based_similarity(code_a, code_b)
        
        # Normalized code similarity (removes whitespace and comments)
        normalized_similarity = self._normalized_similarity(code_a, code_b, language)
        
        # Overall similarity (weighted average)
        overall_similarity = (
            text_similarity * 0.3 +
            structure_similarity * 0.4 +
            normalized_similarity * 0.3
        )
        
        return {
            "overall_similarity": round(overall_similarity, 4),
            "text_similarity": round(text_similarity, 4),
            "structure_similarity": round(structure_similarity, 4),
            "normalized_similarity": round(normalized_similarity, 4),
            "details": self._get_similarity_details(overall_similarity)
        }
    
    def _text_similarity(self, text_a: str, text_b: str) -> float:
        """Calculate text similarity using SequenceMatcher"""
        return difflib.SequenceMatcher(None, text_a, text_b).ratio()
    
    def _normalized_similarity(self, code_a: str, code_b: str, language: str) -> float:
        """Calculate similarity after normalizing code"""
        norm_a = self._normalize_code(code_a, language)
        norm_b = self._normalize_code(code_b, language)
        return difflib.SequenceMatcher(None, norm_a, norm_b).ratio()
    
    def _normalize_code(self, code: str, language: str) -> str:
        """Normalize code by removing comments, whitespace, and formatting"""
        # Remove comments
        if language == 'py':
            code = re.sub(r'#.*', '', code)
            code = re.sub(r'"""[\s\S]*?"""', '', code)
            code = re.sub(r"'''[\s\S]*?'''", '', code)
        elif language in ['js', 'jsx', 'ts', 'tsx', 'java', 'c', 'cpp', 'cs']:
            code = re.sub(r'//.*', '', code)
            code = re.sub(r'/\*[\s\S]*?\*/', '', code)
        
        # Remove extra whitespace
        code = re.sub(r'\s+', ' ', code)
        code = code.strip()
        
        # Convert to lowercase for case-insensitive comparison
        code = code.lower()
        
        return code
    
    def _python_structure_similarity(self, code_a: str, code_b: str) -> float:
        """Calculate structural similarity for Python code using AST"""
        try:
            tree_a = ast.parse(code_a)
            tree_b = ast.parse(code_b)
            
            structure_a = self._extract_python_structure(tree_a)
            structure_b = self._extract_python_structure(tree_b)
            
            # Compare structures
            similarity = difflib.SequenceMatcher(None, structure_a, structure_b).ratio()
            return similarity
        except:
            return 0.0
    
    def _extract_python_structure(self, tree) -> str:
        """Extract structural representation of Python code"""
        structure = []
        
        for node in ast.walk(tree):
            if isinstance(node, ast.FunctionDef):
                structure.append(f"FUNC:{node.name}:{len(node.args.args)}")
            elif isinstance(node, ast.ClassDef):
                structure.append(f"CLASS:{node.name}")
            elif isinstance(node, ast.For):
                structure.append("FOR_LOOP")
            elif isinstance(node, ast.While):
                structure.append("WHILE_LOOP")
            elif isinstance(node, ast.If):
                structure.append("IF_STATEMENT")
            elif isinstance(node, ast.Import):
                for alias in node.names:
                    structure.append(f"IMPORT:{alias.name}")
            elif isinstance(node, ast.ImportFrom):
                structure.append(f"IMPORT_FROM:{node.module}")
        
        return "|".join(structure)
    
    def _token_based_similarity(self, code_a: str, code_b: str) -> float:
        """Calculate similarity based on code tokens"""
        tokens_a = self._tokenize_code(code_a)
        tokens_b = self._tokenize_code(code_b)
        
        return difflib.SequenceMatcher(None, tokens_a, tokens_b).ratio()
    
    def _tokenize_code(self, code: str) -> List[str]:
        """Tokenize code into meaningful tokens"""
        # Split by common delimiters
        tokens = re.findall(r'\w+|[{}()\[\];,.]', code)
        return tokens
    
    def _get_similarity_details(self, similarity: float) -> str:
        """Get human-readable similarity details"""
        if similarity >= 0.9:
            return "Extremely high similarity - likely direct copy"
        elif similarity >= 0.75:
            return "High similarity - possible plagiarism"
        elif similarity >= 0.5:
            return "Moderate similarity - needs review"
        elif similarity >= 0.3:
            return "Low similarity - acceptable"
        else:
            return "Very low similarity - original work"
    
    def get_matching_blocks(self, code_a: str, code_b: str) -> List[Tuple]:
        """Get matching code blocks between two submissions"""
        matcher = difflib.SequenceMatcher(None, code_a, code_b)
        blocks = matcher.get_matching_blocks()
        
        # Filter out small matches
        significant_blocks = [
            (a, b, size) for a, b, size in blocks 
            if size > 20  # Minimum 20 characters
        ]
        
        return significant_blocks
