import ast
import re
from radon.complexity import cc_visit
from radon.metrics import mi_visit, h_visit
from radon.raw import analyze

class CodeAnalyzer:
    """Analyzes code quality, syntax, and metrics"""
    
    def __init__(self, code, language):
        self.code = code
        self.language = language.lower()
        self.errors = []
        self.warnings = []
        self.metrics = {}
    
    def analyze(self):
        """Main analysis method"""
        result = {
            "is_valid": False,
            "language": self.language,
            "errors": [],
            "warnings": [],
            "metrics": {},
            "suggestions": []
        }
        
        try:
            # Language-specific analysis
            if self.language == 'py':
                result = self._analyze_python()
            elif self.language in ['js', 'jsx', 'ts', 'tsx']:
                result = self._analyze_javascript()
            elif self.language == 'java':
                result = self._analyze_java()
            elif self.language in ['c', 'cpp']:
                result = self._analyze_c_cpp()
            elif self.language == 'cs':
                result = self._analyze_csharp()
            else:
                result = self._basic_analysis()
            
            result["code_length"] = len(self.code)
            result["lines_of_code"] = len(self.code.splitlines())
            
        except Exception as e:
            result["errors"].append(f"Analysis error: {str(e)}")
        
        return result
    
    def _analyze_python(self):
        """Analyze Python code"""
        result = {
            "is_valid": True,
            "language": "python",
            "errors": [],
            "warnings": [],
            "metrics": {},
            "suggestions": []
        }
        
        try:
            # Syntax check using AST
            tree = ast.parse(self.code)
            result["is_valid"] = True
            
            # Calculate metrics
            raw_metrics = analyze(self.code)
            result["metrics"] = {
                "lines_of_code": raw_metrics.loc,
                "logical_lines": raw_metrics.lloc,
                "source_lines": raw_metrics.sloc,
                "comments": raw_metrics.comments,
                "blank_lines": raw_metrics.blank,
                "single_comments": raw_metrics.single_comments,
                "multi_comments": raw_metrics.multi
            }
            
            # Complexity analysis
            try:
                complexity = cc_visit(self.code)
                avg_complexity = sum(c.complexity for c in complexity) / len(complexity) if complexity else 0
                result["metrics"]["cyclomatic_complexity"] = round(avg_complexity, 2)
                result["metrics"]["functions_count"] = len(complexity)
                
                # Warn about high complexity
                high_complexity = [c for c in complexity if c.complexity > 10]
                if high_complexity:
                    result["warnings"].append(
                        f"High complexity detected in {len(high_complexity)} function(s)"
                    )
            except:
                pass
            
            # Maintainability Index
            try:
                mi_score = mi_visit(self.code, True)
                result["metrics"]["maintainability_index"] = round(mi_score, 2)
                
                if mi_score < 20:
                    result["warnings"].append("Low maintainability - consider refactoring")
                elif mi_score < 50:
                    result["suggestions"].append("Moderate maintainability - some improvements possible")
            except:
                pass
            
            # Halstead metrics
            try:
                halstead = h_visit(self.code)
                if halstead:
                    result["metrics"]["halstead_difficulty"] = round(halstead.total.difficulty, 2)
                    result["metrics"]["halstead_effort"] = round(halstead.total.effort, 2)
            except:
                pass
            
            # Check for common issues
            self._check_python_patterns(tree, result)
            
        except SyntaxError as e:
            result["is_valid"] = False
            result["errors"].append(f"Syntax Error at line {e.lineno}: {e.msg}")
        except Exception as e:
            result["errors"].append(f"Analysis error: {str(e)}")
        
        return result
    
    def _check_python_patterns(self, tree, result):
        """Check for common Python patterns and issues"""
        # Count imports
        imports = [node for node in ast.walk(tree) if isinstance(node, (ast.Import, ast.ImportFrom))]
        result["metrics"]["imports"] = len(imports)
        
        # Count classes and functions
        classes = [node for node in ast.walk(tree) if isinstance(node, ast.ClassDef)]
        functions = [node for node in ast.walk(tree) if isinstance(node, ast.FunctionDef)]
        
        result["metrics"]["classes"] = len(classes)
        result["metrics"]["functions"] = len(functions)
        
        # Check for docstrings
        missing_docstrings = []
        for func in functions:
            if not ast.get_docstring(func):
                missing_docstrings.append(func.name)
        
        if missing_docstrings and len(missing_docstrings) > len(functions) * 0.5:
            result["suggestions"].append("Consider adding docstrings to functions")
    
    def _analyze_javascript(self):
        """Analyze JavaScript/TypeScript code"""
        result = {
            "is_valid": True,
            "language": "javascript",
            "errors": [],
            "warnings": [],
            "metrics": {},
            "suggestions": []
        }
        
        # Basic syntax checks
        lines = self.code.splitlines()
        result["metrics"]["lines_of_code"] = len(lines)
        
        # Check for common patterns
        function_pattern = r'(function\s+\w+|const\s+\w+\s*=\s*\([^)]*\)\s*=>|\w+\s*:\s*function)'
        functions = re.findall(function_pattern, self.code)
        result["metrics"]["functions"] = len(functions)
        
        # Check for imports/requires
        import_pattern = r'(import\s+.*from|require\()'
        imports = re.findall(import_pattern, self.code)
        result["metrics"]["imports"] = len(imports)
        
        # Check for console.log (potential debug code)
        console_logs = len(re.findall(r'console\.log', self.code))
        if console_logs > 5:
            result["warnings"].append(f"Found {console_logs} console.log statements - consider removing debug code")
        
        # Check for var usage (prefer let/const)
        var_usage = len(re.findall(r'\bvar\s+\w+', self.code))
        if var_usage > 0:
            result["suggestions"].append(f"Consider using 'let' or 'const' instead of 'var' ({var_usage} occurrences)")
        
        # Check for unclosed brackets
        if self.code.count('{') != self.code.count('}'):
            result["errors"].append("Mismatched curly braces")
            result["is_valid"] = False
        
        if self.code.count('(') != self.code.count(')'):
            result["errors"].append("Mismatched parentheses")
            result["is_valid"] = False
        
        return result
    
    def _analyze_java(self):
        """Analyze Java code"""
        result = {
            "is_valid": True,
            "language": "java",
            "errors": [],
            "warnings": [],
            "metrics": {},
            "suggestions": []
        }
        
        lines = self.code.splitlines()
        result["metrics"]["lines_of_code"] = len(lines)
        
        # Check for class definition
        class_pattern = r'(public|private|protected)?\s*class\s+\w+'
        classes = re.findall(class_pattern, self.code)
        result["metrics"]["classes"] = len(classes)
        
        if len(classes) == 0:
            result["warnings"].append("No class definition found")
        
        # Check for methods
        method_pattern = r'(public|private|protected)\s+\w+\s+\w+\s*\([^)]*\)'
        methods = re.findall(method_pattern, self.code)
        result["metrics"]["methods"] = len(methods)
        
        # Check for imports
        import_pattern = r'import\s+[\w.]+;'
        imports = re.findall(import_pattern, self.code)
        result["metrics"]["imports"] = len(imports)
        
        # Basic syntax checks
        if self.code.count('{') != self.code.count('}'):
            result["errors"].append("Mismatched curly braces")
            result["is_valid"] = False
        
        if self.code.count('(') != self.code.count(')'):
            result["errors"].append("Mismatched parentheses")
            result["is_valid"] = False
        
        # Check for main method
        if 'public static void main' in self.code:
            result["metrics"]["has_main"] = True
        
        return result
    
    def _analyze_c_cpp(self):
        """Analyze C/C++ code"""
        result = {
            "is_valid": True,
            "language": "c/c++",
            "errors": [],
            "warnings": [],
            "metrics": {},
            "suggestions": []
        }
        
        lines = self.code.splitlines()
        result["metrics"]["lines_of_code"] = len(lines)
        
        # Check for includes
        include_pattern = r'#include\s*[<"][^>"]+[>"]'
        includes = re.findall(include_pattern, self.code)
        result["metrics"]["includes"] = len(includes)
        
        # Check for functions
        function_pattern = r'\w+\s+\w+\s*\([^)]*\)\s*\{'
        functions = re.findall(function_pattern, self.code)
        result["metrics"]["functions"] = len(functions)
        
        # Check for main function
        if re.search(r'int\s+main\s*\(', self.code):
            result["metrics"]["has_main"] = True
        
        # Basic syntax checks
        if self.code.count('{') != self.code.count('}'):
            result["errors"].append("Mismatched curly braces")
            result["is_valid"] = False
        
        if self.code.count('(') != self.code.count(')'):
            result["errors"].append("Mismatched parentheses")
            result["is_valid"] = False
        
        return result
    
    def _analyze_csharp(self):
        """Analyze C# code"""
        result = {
            "is_valid": True,
            "language": "csharp",
            "errors": [],
            "warnings": [],
            "metrics": {},
            "suggestions": []
        }
        
        lines = self.code.splitlines()
        result["metrics"]["lines_of_code"] = len(lines)
        
        # Check for namespaces
        namespace_pattern = r'namespace\s+[\w.]+'
        namespaces = re.findall(namespace_pattern, self.code)
        result["metrics"]["namespaces"] = len(namespaces)
        
        # Check for classes
        class_pattern = r'(public|private|protected|internal)?\s*class\s+\w+'
        classes = re.findall(class_pattern, self.code)
        result["metrics"]["classes"] = len(classes)
        
        # Check for using statements
        using_pattern = r'using\s+[\w.]+;'
        usings = re.findall(using_pattern, self.code)
        result["metrics"]["using_statements"] = len(usings)
        
        # Basic syntax checks
        if self.code.count('{') != self.code.count('}'):
            result["errors"].append("Mismatched curly braces")
            result["is_valid"] = False
        
        return result
    
    def _basic_analysis(self):
        """Basic analysis for unsupported languages"""
        result = {
            "is_valid": True,
            "language": self.language,
            "errors": [],
            "warnings": ["Basic analysis only - language-specific validation not available"],
            "metrics": {},
            "suggestions": []
        }
        
        lines = self.code.splitlines()
        result["metrics"]["lines_of_code"] = len(lines)
        result["metrics"]["characters"] = len(self.code)
        
        return result
