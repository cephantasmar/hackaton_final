-- Tablas para guardar archivos de entregas de tareas (multi-tenant)

-- Tabla para archivos de UCB
CREATE TABLE IF NOT EXISTS tenant_ucb_assignment_files (
    id BIGSERIAL PRIMARY KEY,
    assignment_id INTEGER NOT NULL,
    student_id INTEGER NOT NULL,
    file_name VARCHAR(255) NOT NULL,
    file_content BYTEA NOT NULL,
    file_size BIGINT NOT NULL,
    content_type VARCHAR(255) NOT NULL,
    is_code_file BOOLEAN DEFAULT FALSE,
    uploaded_at TIMESTAMP DEFAULT NOW(),
    CONSTRAINT fk_ucb_assignment FOREIGN KEY (assignment_id) REFERENCES tenant_ucb_assignments(id) ON DELETE CASCADE,
    CONSTRAINT fk_ucb_student FOREIGN KEY (student_id) REFERENCES tenant_ucb_usuarios(id) ON DELETE CASCADE
);

-- Índices para UCB
CREATE INDEX idx_ucb_files_assignment ON tenant_ucb_assignment_files(assignment_id);
CREATE INDEX idx_ucb_files_student ON tenant_ucb_assignment_files(student_id);
CREATE INDEX idx_ucb_files_code ON tenant_ucb_assignment_files(is_code_file) WHERE is_code_file = TRUE;

-- Tabla para archivos de UPB
CREATE TABLE IF NOT EXISTS tenant_upb_assignment_files (
    id BIGSERIAL PRIMARY KEY,
    assignment_id INTEGER NOT NULL,
    student_id INTEGER NOT NULL,
    file_name VARCHAR(255) NOT NULL,
    file_content BYTEA NOT NULL,
    file_size BIGINT NOT NULL,
    content_type VARCHAR(255) NOT NULL,
    is_code_file BOOLEAN DEFAULT FALSE,
    uploaded_at TIMESTAMP DEFAULT NOW(),
    CONSTRAINT fk_upb_assignment FOREIGN KEY (assignment_id) REFERENCES tenant_upb_assignments(id) ON DELETE CASCADE,
    CONSTRAINT fk_upb_student FOREIGN KEY (student_id) REFERENCES tenant_upb_usuarios(id) ON DELETE CASCADE
);

-- Índices para UPB
CREATE INDEX idx_upb_files_assignment ON tenant_upb_assignment_files(assignment_id);
CREATE INDEX idx_upb_files_student ON tenant_upb_assignment_files(student_id);
CREATE INDEX idx_upb_files_code ON tenant_upb_assignment_files(is_code_file) WHERE is_code_file = TRUE;

-- Tabla para archivos de Gmail
CREATE TABLE IF NOT EXISTS tenant_gmail_assignment_files (
    id BIGSERIAL PRIMARY KEY,
    assignment_id INTEGER NOT NULL,
    student_id INTEGER NOT NULL,
    file_name VARCHAR(255) NOT NULL,
    file_content BYTEA NOT NULL,
    file_size BIGINT NOT NULL,
    content_type VARCHAR(255) NOT NULL,
    is_code_file BOOLEAN DEFAULT FALSE,
    uploaded_at TIMESTAMP DEFAULT NOW(),
    CONSTRAINT fk_gmail_assignment FOREIGN KEY (assignment_id) REFERENCES tenant_gmail_assignments(id) ON DELETE CASCADE,
    CONSTRAINT fk_gmail_student FOREIGN KEY (student_id) REFERENCES tenant_gmail_usuarios(id) ON DELETE CASCADE
);

-- Índices para Gmail
CREATE INDEX idx_gmail_files_assignment ON tenant_gmail_assignment_files(assignment_id);
CREATE INDEX idx_gmail_files_student ON tenant_gmail_assignment_files(student_id);
CREATE INDEX idx_gmail_files_code ON tenant_gmail_assignment_files(is_code_file) WHERE is_code_file = TRUE;

-- Comentarios
COMMENT ON TABLE tenant_ucb_assignment_files IS 'Archivos subidos por estudiantes UCB en entregas de tareas';
COMMENT ON TABLE tenant_upb_assignment_files IS 'Archivos subidos por estudiantes UPB en entregas de tareas';
COMMENT ON TABLE tenant_gmail_assignment_files IS 'Archivos subidos por estudiantes Gmail en entregas de tareas';

COMMENT ON COLUMN tenant_ucb_assignment_files.file_content IS 'Contenido binario del archivo';
COMMENT ON COLUMN tenant_ucb_assignment_files.is_code_file IS 'Indica si el archivo es código fuente';
