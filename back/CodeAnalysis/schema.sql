-- Code Analysis Service Database Schema

-- Table for storing code submissions
CREATE TABLE IF NOT EXISTS code_submissions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    assignment_id TEXT NOT NULL,
    student_id TEXT NOT NULL,
    filename TEXT NOT NULL,
    file_extension TEXT NOT NULL,
    code_content TEXT NOT NULL,
    file_size INTEGER NOT NULL,
    encoding TEXT DEFAULT 'utf-8',
    submitted_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    is_analyzed BOOLEAN DEFAULT FALSE,
    analysis_result JSONB,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Table for storing code analysis results
CREATE TABLE IF NOT EXISTS code_analysis (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    submission_id UUID NOT NULL REFERENCES code_submissions(id) ON DELETE CASCADE,
    is_valid BOOLEAN NOT NULL,
    language TEXT NOT NULL,
    errors JSONB DEFAULT '[]',
    warnings JSONB DEFAULT '[]',
    metrics JSONB DEFAULT '{}',
    suggestions JSONB DEFAULT '[]',
    analyzed_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Table for storing plagiarism reports
CREATE TABLE IF NOT EXISTS plagiarism_reports (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    assignment_id TEXT NOT NULL,
    submission_a_id UUID NOT NULL REFERENCES code_submissions(id) ON DELETE CASCADE,
    submission_b_id UUID NOT NULL REFERENCES code_submissions(id) ON DELETE CASCADE,
    student_a_id TEXT NOT NULL,
    student_b_id TEXT NOT NULL,
    similarity_score DECIMAL(5,4) NOT NULL,
    text_similarity DECIMAL(5,4),
    structure_similarity DECIMAL(5,4),
    is_plagiarism BOOLEAN DEFAULT FALSE,
    details TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Indexes for better performance
CREATE INDEX IF NOT EXISTS idx_submissions_assignment ON code_submissions(assignment_id);
CREATE INDEX IF NOT EXISTS idx_submissions_student ON code_submissions(student_id);
CREATE INDEX IF NOT EXISTS idx_submissions_submitted ON code_submissions(submitted_at DESC);
CREATE INDEX IF NOT EXISTS idx_analysis_submission ON code_analysis(submission_id);
CREATE INDEX IF NOT EXISTS idx_plagiarism_assignment ON plagiarism_reports(assignment_id);
CREATE INDEX IF NOT EXISTS idx_plagiarism_submission_a ON plagiarism_reports(submission_a_id);
CREATE INDEX IF NOT EXISTS idx_plagiarism_submission_b ON plagiarism_reports(submission_b_id);
CREATE INDEX IF NOT EXISTS idx_plagiarism_score ON plagiarism_reports(similarity_score DESC);

-- Trigger to update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

CREATE TRIGGER update_code_submissions_updated_at BEFORE UPDATE ON code_submissions
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_plagiarism_reports_updated_at BEFORE UPDATE ON plagiarism_reports
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Comments
COMMENT ON TABLE code_submissions IS 'Stores student code submissions for assignments';
COMMENT ON TABLE code_analysis IS 'Stores code analysis results including syntax validation and metrics';
COMMENT ON TABLE plagiarism_reports IS 'Stores plagiarism detection results between submissions';
