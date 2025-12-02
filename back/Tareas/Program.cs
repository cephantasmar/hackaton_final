using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using Supabase;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Configuración para Docker - variables de entorno
var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL") 
    ?? "https://nnqbpvbcdwcodnradhye.supabase.co";
var supabaseAnonKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY") 
    ?? "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im5ucWJwdmJjZHdjb2RucmFkaHllIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTg5MzYzMTcsImV4cCI6MjA3NDUxMjMxN30.ZYbcRG9D2J0SlhcT9XTzGX5AAW5wuTXPnzmkbC_pGPU";
var supabaseJwtSecret = Environment.GetEnvironmentVariable("SUPABASE_JWT_SECRET") ?? supabaseAnonKey;
Console.WriteLine("🚀 ========== INICIANDO SERVICIO TAREAS ==========");
Console.WriteLine($"🔗 Supabase URL: {supabaseUrl}");
Console.WriteLine($"🔑 Supabase Key: {supabaseAnonKey?.Substring(0, 20)}...");
Console.WriteLine($"🌍 Environment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");
// 🔹 JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"{supabaseUrl}/auth/v1",
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(supabaseJwtSecret!)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configurar Supabase Client
builder.Services.AddScoped(provider => 
{
    try
    {
        Console.WriteLine($"🔗 Inicializando cliente Supabase: {supabaseUrl}");
        var options = new Supabase.SupabaseOptions
        {
            AutoConnectRealtime = true,
            AutoRefreshToken = true
        };
        
        var client = new Client(supabaseUrl, supabaseAnonKey, options);
        Console.WriteLine("✅ Cliente Supabase inicializado correctamente");
        return client;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error inicializando Supabase: {ex.Message}");
        throw;
    }
});
// 🔹 CORS para Docker
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173", 
                "http://frontend:80", 
                "http://frontend:5173"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
var app = builder.Build();

// Configuración del pipeline
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Health check endpoint
app.MapGet("/health", () => 
{
    return Results.Ok(new { 
        status = "Healthy", 
        service = "Tareas",
        timestamp = DateTime.UtcNow,
        environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
    });
});
// ═══════════════════════════════════════════════════════════════
// 🔹 FUNCIONES HELPER
// ═══════════════════════════════════════════════════════════════

static string GetTenantFromEmail(string email)
{
    Console.WriteLine($"🔍 Obteniendo tenant para email: {email}");
    if (email.EndsWith("@ucb.edu.bo")) 
    {
        Console.WriteLine("✅ Tenant identificado: ucb.edu.bo");
        return "ucb.edu.bo";
    }
    if (email.EndsWith("@upb.edu.bo")) 
    {
        Console.WriteLine("✅ Tenant identificado: upb.edu.bo");
        return "upb.edu.bo";
    }
    if (email.EndsWith("@gmail.com")) 
    {
        Console.WriteLine("✅ Tenant identificado: gmail.com");
        return "gmail.com";
    }
    Console.WriteLine("❌ Tenant no identificado");
    return "unknown";
}

async Task<UserInfo?> GetUserFromToken(string email)
{
    Console.WriteLine($"👤 Obteniendo información del usuario: {email}");
    try
    {
        var tenant = GetTenantFromEmail(email);
        if (tenant == "unknown") 
        {
            Console.WriteLine("❌ Tenant desconocido, no se puede obtener usuario");
            return null;
        }

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("apikey", supabaseAnonKey);
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseAnonKey}");

        Console.WriteLine($"🔍 Buscando tenant en: {supabaseUrl}/rest/v1/tenants?domain=eq.{tenant}");
        var tenantResponse = await httpClient.GetAsync($"{supabaseUrl}/rest/v1/tenants?domain=eq.{tenant}&select=*");
        
        Console.WriteLine($"📡 Respuesta tenant: {tenantResponse.StatusCode}");
        if (!tenantResponse.IsSuccessStatusCode) 
        {
            Console.WriteLine($"❌ Error obteniendo tenant: {tenantResponse.StatusCode}");
            return null;
        }

        var tenantContent = await tenantResponse.Content.ReadAsStringAsync();
        Console.WriteLine($"📋 Contenido tenant: {tenantContent}");
        
        var tenants = JsonSerializer.Deserialize<JsonElement[]>(tenantContent);
        if (tenants?.Length == 0) 
        {
            Console.WriteLine("❌ No se encontró el tenant");
            return null;
        }

        var schema = tenants[0].GetProperty("schema_name").GetString();
        Console.WriteLine($"✅ Schema identificado: {schema}");

        var userResponse = await httpClient.GetAsync($"{supabaseUrl}/rest/v1/{schema}_usuarios?email=eq.{Uri.EscapeDataString(email)}&select=id,rol");
        Console.WriteLine($"📡 Respuesta usuario: {userResponse.StatusCode}");
        
        if (!userResponse.IsSuccessStatusCode) 
        {
            Console.WriteLine($"❌ Error obteniendo usuario: {userResponse.StatusCode}");
            return null;
        }

        var userContent = await userResponse.Content.ReadAsStringAsync();
        Console.WriteLine($"📋 Contenido usuario: {userContent}");
        
        var users = JsonSerializer.Deserialize<JsonElement[]>(userContent);
        if (users?.Length == 0) 
        {
            Console.WriteLine("❌ No se encontró el usuario");
            return null;
        }

        var user = users[0];
        var userId = user.GetProperty("id").GetInt32();
        var userRole = user.GetProperty("rol").GetString() ?? "Estudiante";

        Console.WriteLine($"✅ Usuario encontrado: ID={userId}, Rol={userRole}");
        return new UserInfo(userId, userRole);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error obteniendo información del usuario: {ex.Message}");
        Console.WriteLine($"💥 Stack trace: {ex.StackTrace}");
        return null;
    }
}
async Task<List<AssignmentWithCourseInfo>> GetAllUserAssignments(Client supabase, int userId, string userRole, string tenant)
{
    Console.WriteLine($"📚 Obteniendo todas las tareas del usuario: {userId}, Rol: {userRole}, Tenant: {tenant}");
    var assignmentsWithCourse = new List<AssignmentWithCourseInfo>();
    
    try
    {
        if (userRole == "Profesor")
        {
            Console.WriteLine("👨‍🏫 Obteniendo tareas creadas por el profesor...");
            var assignedCourses = await GetProfessorCourses(supabase, userId, tenant);
            Console.WriteLine($"📖 Cursos asignados al profesor: {assignedCourses.Count}");
            
            foreach (var course in assignedCourses)
            {
                var courseAssignments = await GetCourseAssignments(supabase, course.Id, tenant);
                foreach (var assignment in courseAssignments)
                {
                    assignmentsWithCourse.Add(new AssignmentWithCourseInfo(
                        assignment, course.Nombre, course.Codigo
                    ));
                }
            }
        }
        else if (userRole == "Estudiante")
        {
            Console.WriteLine("👨‍🎓 Obteniendo tareas del estudiante...");
            var enrolledCourses = await GetStudentCourses(supabase, userId, tenant);
            Console.WriteLine($"📖 Cursos inscritos del estudiante: {enrolledCourses.Count}");
            
            foreach (var course in enrolledCourses)
            {
                var courseAssignments = await GetCourseAssignments(supabase, course.Id, tenant);
                foreach (var assignment in courseAssignments)
                {
                    assignmentsWithCourse.Add(new AssignmentWithCourseInfo(
                        assignment, course.Nombre, course.Codigo
                    ));
                }
            }
        }
        
        Console.WriteLine($"✅ Total de tareas obtenidas: {assignmentsWithCourse.Count}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error obteniendo todas las tareas: {ex.Message}");
    }
    
    return assignmentsWithCourse;
}

async Task<CompletionInfo?> GetAssignmentCompletion(Client supabase, int assignmentId, int studentId, string tenant)
{
    Console.WriteLine($"✅ Obteniendo completion - Assignment: {assignmentId}, Student: {studentId}");
    try
    {
        switch (tenant.ToLower())
        {
            case "ucb.edu.bo":
                var responseUcb = await supabase.From<AssignmentCompletionUcb>()
                    .Where(x => x.AssignmentId == assignmentId && x.StudentId == studentId)
                    .Get();
                var completionUcb = responseUcb.Models.FirstOrDefault();
                Console.WriteLine(completionUcb != null ? "✅ Completion encontrado UCB" : "❌ Completion no encontrado UCB");
                return completionUcb != null ? new CompletionInfo(
                    completionUcb.CompletedAt, completionUcb.Status, completionUcb.SubmittedContent
                ) : null;
                
            case "upb.edu.bo":
                var responseUpb = await supabase.From<AssignmentCompletionUpb>()
                    .Where(x => x.AssignmentId == assignmentId && x.StudentId == studentId)
                    .Get();
                var completionUpb = responseUpb.Models.FirstOrDefault();
                Console.WriteLine(completionUpb != null ? "✅ Completion encontrado UPB" : "❌ Completion no encontrado UPB");
                return completionUpb != null ? new CompletionInfo(
                    completionUpb.CompletedAt, completionUpb.Status, completionUpb.SubmittedContent
                ) : null;
                
            case "gmail.com":
                var responseGmail = await supabase.From<AssignmentCompletionGmail>()
                    .Where(x => x.AssignmentId == assignmentId && x.StudentId == studentId)
                    .Get();
                var completionGmail = responseGmail.Models.FirstOrDefault();
                Console.WriteLine(completionGmail != null ? "✅ Completion encontrado Gmail" : "❌ Completion no encontrado Gmail");
                return completionGmail != null ? new CompletionInfo(
                    completionGmail.CompletedAt, completionGmail.Status, completionGmail.SubmittedContent
                ) : null;
                
            default:
                Console.WriteLine("❌ Tenant no soportado para obtener completion");
                return null;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error obteniendo completion: {ex.Message}");
        return null;
    }
}

async Task<CompletionStats> GetAssignmentCompletionStats(Client supabase, int assignmentId, string tenant)
{
    Console.WriteLine($"📊 Obteniendo stats - Assignment: {assignmentId}");
    try
    {
        switch (tenant.ToLower())
        {
            case "ucb.edu.bo":
                var responseUcb = await supabase.From<AssignmentCompletionUcb>()
                    .Where(x => x.AssignmentId == assignmentId)
                    .Get();
                var completionsUcb = responseUcb.Models;
                var completedUcb = completionsUcb.Count(c => c.Status == "completed");
                Console.WriteLine($"✅ Stats UCB: {completedUcb}/{completionsUcb.Count}");
                return new CompletionStats(completionsUcb.Count, completedUcb);
                
            case "upb.edu.bo":
                var responseUpb = await supabase.From<AssignmentCompletionUpb>()
                    .Where(x => x.AssignmentId == assignmentId)
                    .Get();
                var completionsUpb = responseUpb.Models;
                var completedUpb = completionsUpb.Count(c => c.Status == "completed");
                Console.WriteLine($"✅ Stats UPB: {completedUpb}/{completionsUpb.Count}");
                return new CompletionStats(completionsUpb.Count, completedUpb);
                
            case "gmail.com":
                var responseGmail = await supabase.From<AssignmentCompletionGmail>()
                    .Where(x => x.AssignmentId == assignmentId)
                    .Get();
                var completionsGmail = responseGmail.Models;
                var completedGmail = completionsGmail.Count(c => c.Status == "completed");
                Console.WriteLine($"✅ Stats Gmail: {completedGmail}/{completionsGmail.Count}");
                return new CompletionStats(completionsGmail.Count, completedGmail);
                
            default:
                Console.WriteLine("❌ Tenant no soportado para stats");
                return new CompletionStats(0, 0);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error obteniendo stats: {ex.Message}");
        return new CompletionStats(0, 0);
    }
}

async Task<bool> IsUserEnrolledInCourse(Client supabase, int userId, int courseId, string tenant, string userRole)
{
    Console.WriteLine($"📚 Verificando acceso - User: {userId}, Course: {courseId}, Tenant: {tenant}, Role: {userRole}");
    
    try
    {
        if (userRole == "Profesor")
        {
            Console.WriteLine("👨‍🏫 Verificando asignación de profesor al curso...");
            switch (tenant.ToLower())
            {
                case "ucb.edu.bo":
                    var cursoUcb = await supabase.From<CursoUcb>()
                        .Where(x => x.Id == courseId && x.ProfesorId == userId)
                        .Get();
                    var isProfesorUcb = cursoUcb.Models.Any();
                    Console.WriteLine(isProfesorUcb ? "✅ Profesor asignado al curso UCB" : "❌ Profesor NO asignado al curso UCB");
                    return isProfesorUcb;
                    
                case "upb.edu.bo":
                    var cursoUpb = await supabase.From<CursoUpb>()
                        .Where(x => x.Id == courseId && x.ProfesorId == userId)
                        .Get();
                    var isProfesorUpb = cursoUpb.Models.Any();
                    Console.WriteLine(isProfesorUpb ? "✅ Profesor asignado al curso UPB" : "❌ Profesor NO asignado al curso UPB");
                    return isProfesorUpb;
                    
                case "gmail.com":
                    var cursoGmail = await supabase.From<CursoGmail>()
                        .Where(x => x.Id == courseId && x.ProfesorId == userId)
                        .Get();
                    var isProfesorGmail = cursoGmail.Models.Any();
                    Console.WriteLine(isProfesorGmail ? "✅ Profesor asignado al curso Gmail" : "❌ Profesor NO asignado al curso Gmail");
                    return isProfesorGmail;
                    
                default:
                    Console.WriteLine("❌ Tenant no soportado para verificación de profesor");
                    return false;
            }
        }
        else if (userRole == "Estudiante")
        {
            Console.WriteLine("👨‍🎓 Verificando inscripción de estudiante...");
            switch (tenant.ToLower())
            {
                case "ucb.edu.bo":
                    var responseUcb = await supabase.From<InscripcionUcb>()
                        .Where(x => x.UsuarioId == userId && x.CursoId == courseId)
                        .Get();
                    var isEnrolledUcb = responseUcb.Models.Any();
                    Console.WriteLine(isEnrolledUcb ? "✅ Estudiante inscrito en curso UCB" : "❌ Estudiante NO inscrito en curso UCB");
                    return isEnrolledUcb;
                    
                case "upb.edu.bo":
                    var responseUpb = await supabase.From<InscripcionUpb>()
                        .Where(x => x.UsuarioId == userId && x.CursoId == courseId)
                        .Get();
                    var isEnrolledUpb = responseUpb.Models.Any();
                    Console.WriteLine(isEnrolledUpb ? "✅ Estudiante inscrito en curso UPB" : "❌ Estudiante NO inscrito en curso UPB");
                    return isEnrolledUpb;
                    
                case "gmail.com":
                    var responseGmail = await supabase.From<InscripcionGmail>()
                        .Where(x => x.UsuarioId == userId && x.CursoId == courseId)
                        .Get();
                    var isEnrolledGmail = responseGmail.Models.Any();
                    Console.WriteLine(isEnrolledGmail ? "✅ Estudiante inscrito en curso Gmail" : "❌ Estudiante NO inscrito en curso Gmail");
                    return isEnrolledGmail;
                    
                default:
                    Console.WriteLine("❌ Tenant no soportado para verificación de estudiante");
                    return false;
            }
        }
        
        Console.WriteLine("❌ Rol no reconocido");
        return false;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error verificando acceso: {ex.Message}");
        return false;
    }
}

async Task<List<AssignmentInfo>> GetCourseAssignments(Client supabase, int courseId, string tenant)
{
    Console.WriteLine($"🔍 BUSCANDO TAREAS - Curso: {courseId}, Tenant: {tenant}");
    var assignments = new List<AssignmentInfo>();

    try
    {
        switch (tenant.ToLower())
        {
            case "ucb.edu.bo":
                Console.WriteLine("🔍 Ejecutando query para UCB...");
                var responseUcb = await supabase.From<AssignmentUcb>()
                    .Where(x => x.CursoId == courseId && x.IsActive == true)
                    .Get();

                Console.WriteLine($"📊 Tareas UCB encontradas: {responseUcb.Models.Count}");
                Console.WriteLine($"🔍 SQL ejecutado: {responseUcb.ResponseMessage?.RequestMessage?.RequestUri}");

                if (responseUcb.Models.Count > 0)
                {
                    foreach (var assignment in responseUcb.Models)
                    {
                        Console.WriteLine($"📝 Tarea UCB: ID={assignment.Id}, Title='{assignment.Title}', Curso={assignment.CursoId}");
                    }
                }

                assignments = responseUcb.Models.Select(a => new AssignmentInfo(
                    a.Id, a.Title, a.Description, a.DueDate, a.Points,
                    a.AssignmentType, a.CursoId, a.CreatedAt
                )).ToList();
                break;

            case "upb.edu.bo":
                Console.WriteLine("🔍 Ejecutando query para UPB...");
                var responseUpb = await supabase.From<AssignmentUpb>()
                    .Where(x => x.CursoId == courseId && x.IsActive == true)
                    .Get();

                Console.WriteLine($"📊 Tareas UPB encontradas: {responseUpb.Models.Count}");
                Console.WriteLine($"🔍 SQL ejecutado: {responseUpb.ResponseMessage?.RequestMessage?.RequestUri}");

                if (responseUpb.Models.Count > 0)
                {
                    foreach (var assignment in responseUpb.Models)
                    {
                        Console.WriteLine($"📝 Tarea UPB: ID={assignment.Id}, Title='{assignment.Title}', Curso={assignment.CursoId}");
                    }
                }

                assignments = responseUpb.Models.Select(a => new AssignmentInfo(
                    a.Id, a.Title, a.Description, a.DueDate, a.Points,
                    a.AssignmentType, a.CursoId, a.CreatedAt
                )).ToList();
                break;

            case "gmail.com":
                Console.WriteLine("🔍 Ejecutando query para Gmail...");
                var responseGmail = await supabase.From<AssignmentGmail>()
                    .Where(x => x.CursoId == courseId && x.IsActive == true)
                    .Get();

                Console.WriteLine($"📊 Tareas Gmail encontradas: {responseGmail.Models.Count}");
                Console.WriteLine($"🔍 SQL ejecutado: {responseGmail.ResponseMessage?.RequestMessage?.RequestUri}");

                if (responseGmail.Models.Count > 0)
                {
                    foreach (var assignment in responseGmail.Models)
                    {
                        Console.WriteLine($"📝 Tarea Gmail: ID={assignment.Id}, Title='{assignment.Title}', Curso={assignment.CursoId}");
                    }
                }

                assignments = responseGmail.Models.Select(a => new AssignmentInfo(
                    a.Id, a.Title, a.Description, a.DueDate, a.Points,
                    a.AssignmentType, a.CursoId, a.CreatedAt
                )).ToList();
                break;

            default:
                Console.WriteLine($"❌ Tenant no soportado: {tenant}");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error obteniendo tareas: {ex.Message}");
        Console.WriteLine($"💥 Stack trace: {ex.StackTrace}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"💥 Inner exception: {ex.InnerException.Message}");
        }
    }

    Console.WriteLine($"✅ Tareas retornadas: {assignments.Count}");
    return assignments;
}

async Task<List<CourseInfo>> GetProfessorCourses(Client supabase, int profesorId, string tenant)
{
    Console.WriteLine($"👨‍🏫 Obteniendo cursos del profesor: {profesorId}, Tenant: {tenant}");
    var courses = new List<CourseInfo>();
    
    try
    {
        switch (tenant.ToLower())
        {
            case "ucb.edu.bo":
                var responseUcb = await supabase.From<CursoUcb>()
                    .Where(x => x.ProfesorId == profesorId)
                    .Get();
                Console.WriteLine($"📚 Cursos UCB encontrados: {responseUcb.Models.Count}");
                courses = responseUcb.Models.Select(c => new CourseInfo(c.Id, c.Nombre ?? "Sin nombre", c.Codigo ?? "Sin código")).ToList();
                break;
                
            case "upb.edu.bo":
                var responseUpb = await supabase.From<CursoUpb>()
                    .Where(x => x.ProfesorId == profesorId)
                    .Get();
                Console.WriteLine($"📚 Cursos UPB encontrados: {responseUpb.Models.Count}");
                courses = responseUpb.Models.Select(c => new CourseInfo(c.Id, c.Nombre ?? "Sin nombre", c.Codigo ?? "Sin código")).ToList();
                break;
                
            case "gmail.com":
                var responseGmail = await supabase.From<CursoGmail>()
                    .Where(x => x.ProfesorId == profesorId)
                    .Get();
                Console.WriteLine($"📚 Cursos Gmail encontrados: {responseGmail.Models.Count}");
                courses = responseGmail.Models.Select(c => new CourseInfo(c.Id, c.Nombre ?? "Sin nombre", c.Codigo ?? "Sin código")).ToList();
                break;
        }
        
        Console.WriteLine($"✅ Cursos del profesor: {courses.Count}");
        foreach (var course in courses)
        {
            Console.WriteLine($"📖 Curso: ID={course.Id}, Nombre='{course.Nombre}'");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error obteniendo cursos del profesor: {ex.Message}");
        Console.WriteLine($"💥 Stack trace: {ex.StackTrace}");
    }
    
    return courses;
}
async Task<List<CourseInfo>> GetStudentCourses(Client supabase, int studentId, string tenant)
{
    Console.WriteLine($"👨‍🎓 Obteniendo cursos del estudiante: {studentId}, Tenant: {tenant}");
    var courses = new List<CourseInfo>();
    
    try
    {
        switch (tenant.ToLower())
        {
            case "ucb.edu.bo":
                var responseUcb = await supabase.From<InscripcionUcb>()
                    .Where(x => x.UsuarioId == studentId)
                    .Get();
                var inscripcionesUcb = responseUcb.Models;
                Console.WriteLine($"📋 Inscripciones UCB encontradas: {inscripcionesUcb.Count}");
                
                foreach (var insc in inscripcionesUcb)
                {
                    var courseUcb = await supabase.From<CursoUcb>()
                        .Where(x => x.Id == insc.CursoId)
                        .Get();
                    var cursoUcb = courseUcb.Models.FirstOrDefault();
                    if (cursoUcb != null)
                        courses.Add(new CourseInfo(cursoUcb.Id, cursoUcb.Nombre ?? "Sin nombre", cursoUcb.Codigo ?? "Sin código"));
                }
                break;
                
            case "upb.edu.bo":
                var responseUpb = await supabase.From<InscripcionUpb>()
                    .Where(x => x.UsuarioId == studentId)
                    .Get();
                var inscripcionesUpb = responseUpb.Models;
                Console.WriteLine($"📋 Inscripciones UPB encontradas: {inscripcionesUpb.Count}");
                
                foreach (var insc in inscripcionesUpb)
                {
                    var courseUpb = await supabase.From<CursoUpb>()
                        .Where(x => x.Id == insc.CursoId)
                        .Get();
                    var cursoUpb = courseUpb.Models.FirstOrDefault();
                    if (cursoUpb != null)
                        courses.Add(new CourseInfo(cursoUpb.Id, cursoUpb.Nombre ?? "Sin nombre", cursoUpb.Codigo ?? "Sin código"));
                }
                break;
                
            case "gmail.com":
                var responseGmail = await supabase.From<InscripcionGmail>()
                    .Where(x => x.UsuarioId == studentId)
                    .Get();
                var inscripcionesGmail = responseGmail.Models;
                Console.WriteLine($"📋 Inscripciones Gmail encontradas: {inscripcionesGmail.Count}");
                
                foreach (var insc in inscripcionesGmail)
                {
                    var courseGmail = await supabase.From<CursoGmail>()
                        .Where(x => x.Id == insc.CursoId)
                        .Get();
                    var cursoGmail = courseGmail.Models.FirstOrDefault();
                    if (cursoGmail != null)
                        courses.Add(new CourseInfo(cursoGmail.Id, cursoGmail.Nombre ?? "Sin nombre", cursoGmail.Codigo ?? "Sin código"));
                }
                break;
        }
        
        Console.WriteLine($"✅ Cursos del estudiante: {courses.Count}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error obteniendo cursos del estudiante: {ex.Message}");
    }
    
    return courses;
}

async Task<string?> GetCourseName(Client supabase, int courseId, string tenant)
{
    try
    {
        switch (tenant.ToLower())
        {
            case "ucb.edu.bo":
                var responseUcb = await supabase.From<CursoUcb>()
                    .Where(x => x.Id == courseId)
                    .Get();
                return responseUcb.Models.FirstOrDefault()?.Nombre;
                
            case "upb.edu.bo":
                var responseUpb = await supabase.From<CursoUpb>()
                    .Where(x => x.Id == courseId)
                    .Get();
                return responseUpb.Models.FirstOrDefault()?.Nombre;
                
            case "gmail.com":
                var responseGmail = await supabase.From<CursoGmail>()
                    .Where(x => x.Id == courseId)
                    .Get();
                return responseGmail.Models.FirstOrDefault()?.Nombre;
                
            default:
                return null;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error obteniendo nombre del curso: {ex.Message}");
        return null;
    }
}

async Task<bool> VerifyCourseExists(Client supabase, int courseId, string tenant)
{
    try
    {
        switch (tenant.ToLower())
        {
            case "ucb.edu.bo":
                var responseUcb = await supabase.From<CursoUcb>()
                    .Where(x => x.Id == courseId)
                    .Get();
                return responseUcb.Models.Any();
                
            case "upb.edu.bo":
                var responseUpb = await supabase.From<CursoUpb>()
                    .Where(x => x.Id == courseId)
                    .Get();
                return responseUpb.Models.Any();
                
            case "gmail.com":
                var responseGmail = await supabase.From<CursoGmail>()
                    .Where(x => x.Id == courseId)
                    .Get();
                return responseGmail.Models.Any();
                
            default:
                return false;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error verificando curso: {ex.Message}");
        return false;
    }
}

async Task<int> CreateAssignment(Client supabase, AssignmentRequest request, int profesorId, string tenant)
{
    Console.WriteLine($"➕ CREANDO TAREA - Validando datos...");
    Console.WriteLine($"📦 Request: Title='{request.Title}', CourseId={request.CursoId}, Type='{request.AssignmentType}'");

    try
    {
        // Validación más robusta
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            Console.WriteLine("❌ ERROR: Título vacío");
            return 0;
        }

        if (request.CursoId <= 0)
        {
            Console.WriteLine($"❌ ERROR: CourseId inválido: {request.CursoId}");
            return 0;
        }

        // Verificar que el curso existe
        var courseExists = await VerifyCourseExists(supabase, request.CursoId, tenant);
        if (!courseExists)
        {
            Console.WriteLine($"❌ ERROR: El curso {request.CursoId} no existe");
            return 0;
        }

        var assignmentType = string.IsNullOrEmpty(request.AssignmentType) ? "tarea" : request.AssignmentType;

        Console.WriteLine($"📦 Insertando en tenant: {tenant}");
        Console.WriteLine($"📝 Datos: Title='{request.Title}', Type='{assignmentType}', Course={request.CursoId}, Profesor={profesorId}");

        switch (tenant.ToLower())
        {
            case "ucb.edu.bo":
                var assignmentUcb = new AssignmentUcb
                {
                    Title = request.Title.Trim(),
                    Description = request.Description?.Trim() ?? "",
                    DueDate = request.DueDate,
                    Points = request.Points,
                    AssignmentType = assignmentType,
                    CursoId = request.CursoId,
                    ProfesorId = profesorId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true,
                    Status = "active"
                };

                Console.WriteLine("📤 Ejecutando INSERT en UCB...");
                var responseUcb = await supabase.From<AssignmentUcb>().Insert(assignmentUcb);

                if (responseUcb.ResponseMessage?.IsSuccessStatusCode == true)
                {
                    var newAssignment = responseUcb.Models.FirstOrDefault();
                    if (newAssignment != null && newAssignment.Id > 0)
                    {
                        Console.WriteLine($"✅ Tarea UCB creada con ID: {newAssignment.Id}");
                        return newAssignment.Id;
                    }
                    else
                    {
                        Console.WriteLine("⚠️ Tarea creada pero no se pudo obtener ID, verificando directamente...");
                        // Intentar obtener la tarea recién creada
                        var verifyResponse = await supabase.From<AssignmentUcb>()
                            .Where(x => x.ProfesorId == profesorId && x.CursoId == request.CursoId)
                            .Order(x => x.CreatedAt, Supabase.Postgrest.Constants.Ordering.Descending)
                            .Limit(1)
                            .Get();

                        var latest = verifyResponse.Models.FirstOrDefault();
                        if (latest != null)
                        {
                            Console.WriteLine($"✅ Tarea verificada con ID: {latest.Id}");
                            return latest.Id;
                        }
                    }
                }

                Console.WriteLine($"❌ Error en UCB: {responseUcb.ResponseMessage?.StatusCode}");
                return 0;

            case "upb.edu.bo":
                var assignmentUpb = new AssignmentUpb
                {
                    Title = request.Title.Trim(),
                    Description = request.Description?.Trim() ?? "",
                    DueDate = request.DueDate,
                    Points = request.Points,
                    AssignmentType = assignmentType,
                    CursoId = request.CursoId,
                    ProfesorId = profesorId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true,
                    Status = "active"
                };

                Console.WriteLine("📤 Ejecutando INSERT en UPB...");
                var responseUpb = await supabase.From<AssignmentUpb>().Insert(assignmentUpb);

                if (responseUpb.ResponseMessage?.IsSuccessStatusCode == true)
                {
                    var newAssignment = responseUpb.Models.FirstOrDefault();
                    if (newAssignment != null && newAssignment.Id > 0)
                    {
                        Console.WriteLine($"✅ Tarea UPB creada con ID: {newAssignment.Id}");
                        return newAssignment.Id;
                    }
                }

                Console.WriteLine($"❌ Error en UPB: {responseUpb.ResponseMessage?.StatusCode}");
                return 0;

            case "gmail.com":
                var assignmentGmail = new AssignmentGmail
                {
                    Title = request.Title.Trim(),
                    Description = request.Description?.Trim() ?? "",
                    DueDate = request.DueDate,
                    Points = request.Points,
                    AssignmentType = assignmentType,
                    CursoId = request.CursoId,
                    ProfesorId = profesorId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true,
                    Status = "active"
                };

                Console.WriteLine("📤 Ejecutando INSERT en Gmail...");
                var responseGmail = await supabase.From<AssignmentGmail>().Insert(assignmentGmail);

                if (responseGmail.ResponseMessage?.IsSuccessStatusCode == true)
                {
                    var newAssignment = responseGmail.Models.FirstOrDefault();
                    if (newAssignment != null && newAssignment.Id > 0)
                    {
                        Console.WriteLine($"✅ Tarea Gmail creada con ID: {newAssignment.Id}");
                        return newAssignment.Id;
                    }
                    else
                    {
                        Console.WriteLine("⚠️ Tarea creada pero ID no retornado, verificando...");
                        // Verificar la tarea recién creada
                        var verifyResponse = await supabase.From<AssignmentGmail>()
                            .Where(x => x.ProfesorId == profesorId && x.CursoId == request.CursoId)
                            .Order(x => x.CreatedAt, Supabase.Postgrest.Constants.Ordering.Descending)
                            .Limit(1)
                            .Get();

                        var latest = verifyResponse.Models.FirstOrDefault();
                        if (latest != null)
                        {
                            Console.WriteLine($"✅ Tarea Gmail verificada con ID: {latest.Id}");
                            return latest.Id;
                        }
                    }
                }

                Console.WriteLine($"❌ Error en Gmail: {responseGmail.ResponseMessage?.StatusCode}");
                if (responseGmail.Content != null)
                {
                    Console.WriteLine($"❌ Contenido de error: {responseGmail.Content}");
                }
                return 0;

            default:
                Console.WriteLine("❌ Tenant no soportado");
                return 0;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error CRÍTICO creando tarea: {ex.Message}");
        Console.WriteLine($"💥 Stack trace: {ex.StackTrace}");
        Console.WriteLine($"💥 Inner exception: {ex.InnerException?.Message}");
        return 0;
    }
}

// ═══════════════════════════════════════════════════════════════
// 🔹 ENDPOINTS PRINCIPALES
// ═══════════════════════════════════════════════════════════════

app.MapGet("/", () => "Tareas Service is running!");

app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "Tareas", timestamp = DateTime.Now }));

// 🔹 OBTENER TODAS LAS TAREAS DEL USUARIO (VISTA GENERAL)
app.MapGet("/api/assignments", async (HttpContext context, [FromServices] Client supabase) =>
{
    Console.WriteLine("📝 Endpoint: Obtener todas las tareas del usuario");
    try
    {
        var email = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
        if (string.IsNullOrEmpty(email))
        {
            Console.WriteLine("❌ Usuario no autenticado");
            return Results.Unauthorized();
        }

        Console.WriteLine($"🎯 Obteniendo tareas para usuario: {email}");

        var user = await GetUserFromToken(email);
        if (user == null) 
        {
            Console.WriteLine("❌ Usuario no encontrado en base de datos");
            return Results.NotFound("Usuario no encontrado");
        }

        var tenant = GetTenantFromEmail(email);
        if (tenant == "unknown")
        {
            Console.WriteLine("❌ Tenant no identificado");
            return Results.BadRequest("Tenant no identificado");
        }

        var userId = user.Id;
        var userRole = user.Rol;

        Console.WriteLine($"👤 Usuario: {userId}, Rol: {userRole}, Tenant: {tenant}");

        var allAssignments = await GetAllUserAssignments(supabase, userId, userRole, tenant);
        
        var result = new List<object>();

        foreach (var assignmentWithCourse in allAssignments)
        {
            var assignment = assignmentWithCourse.Assignment;
            
            if (userRole == "Estudiante")
            {
                var completion = await GetAssignmentCompletion(supabase, assignment.Id, userId, tenant);
                result.Add(new
                {
                    assignment = new {
                        id = assignment.Id,
                        title = assignment.Title,
                        description = assignment.Description,
                        due_date = assignment.DueDate,
                        points = assignment.Points,
                        assignment_type = assignment.AssignmentType,
                        curso_id = assignment.CursoId,
                        created_at = assignment.CreatedAt,
                        curso_nombre = assignmentWithCourse.CursoNombre,
                        curso_codigo = assignmentWithCourse.CursoCodigo
                    },
                    completion = completion
                });
            }
            else if (userRole == "Profesor")
            {
                var completionStats = await GetAssignmentCompletionStats(supabase, assignment.Id, tenant);
                result.Add(new
                {
                    assignment = new {
                        id = assignment.Id,
                        title = assignment.Title,
                        description = assignment.Description,
                        due_date = assignment.DueDate,
                        points = assignment.Points,
                        assignment_type = assignment.AssignmentType,
                        curso_id = assignment.CursoId,
                        created_at = assignment.CreatedAt,
                        curso_nombre = assignmentWithCourse.CursoNombre,
                        curso_codigo = assignmentWithCourse.CursoCodigo
                    },
                    completions = completionStats
                });
            }
        }

        Console.WriteLine($"✅ Tareas obtenidas: {result.Count}");

        return Results.Ok(new { 
            assignments = result, 
            userRole,
            total = result.Count 
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error obteniendo todas las tareas: {ex.Message}");
        Console.WriteLine($"💥 Stack trace: {ex.StackTrace}");
        return Results.Problem("Error interno del servidor");
    }
}).RequireAuthorization();

// 🔹 OBTENER TAREAS DE UN CURSO ESPECÍFICO
app.MapGet("/api/courses/{courseId}/assignments", async (HttpContext context, int courseId, [FromServices] Client supabase) =>
{
    Console.WriteLine($"📝 Endpoint: Obtener tareas del curso {courseId}");
    
    try
    {
        var email = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
        if (string.IsNullOrEmpty(email))
        {
            Console.WriteLine("❌ Usuario no autenticado");
            return Results.Unauthorized();
        }

        Console.WriteLine($"🎯 Usuario: {email}, Curso: {courseId}");

        var user = await GetUserFromToken(email);
        if (user == null) 
        {
            Console.WriteLine("❌ Usuario no encontrado");
            return Results.NotFound("Usuario no encontrado");
        }

        var tenant = GetTenantFromEmail(email);
        if (tenant == "unknown") 
        {
            Console.WriteLine("❌ Tenant no identificado");
            return Results.BadRequest("Tenant no identificado");
        }

        Console.WriteLine($"👤 Usuario: ID={user.Id}, Rol={user.Rol}, Tenant={tenant}");

        // Verificar que el usuario tiene acceso al curso
        var hasAccess = await IsUserEnrolledInCourse(supabase, user.Id, courseId, tenant, user.Rol);
        Console.WriteLine($"🔐 Usuario tiene acceso al curso: {hasAccess}");
        
        if (!hasAccess)
        {
            Console.WriteLine("❌ Usuario no tiene acceso al curso");
            return Results.Problem("No tienes acceso a este curso");
        }

        Console.WriteLine($"🔍 Obteniendo tareas para curso {courseId}");
        var assignments = await GetCourseAssignments(supabase, courseId, tenant);
        
        // Obtener nombre del curso
        var courseName = await GetCourseName(supabase, courseId, tenant);
        Console.WriteLine($"📚 Nombre del curso: {courseName}");
        
        // Para estudiantes, agregar información de completion
        var result = new List<object>();
        foreach (var assignment in assignments)
        {
            if (user.Rol == "Estudiante")
            {
                var completion = await GetAssignmentCompletion(supabase, assignment.Id, user.Id, tenant);
                result.Add(new
                {
                    assignment = new {
                        id = assignment.Id,
                        title = assignment.Title,
                        description = assignment.Description,
                        due_date = assignment.DueDate,
                        points = assignment.Points,
                        assignment_type = assignment.AssignmentType,
                        curso_id = assignment.CursoId,
                        created_at = assignment.CreatedAt,
                        curso_nombre = courseName
                    },
                    completion = completion
                });
            }
            else if (user.Rol == "Profesor")
            {
                var completionStats = await GetAssignmentCompletionStats(supabase, assignment.Id, tenant);
                result.Add(new
                {
                    assignment = new {
                        id = assignment.Id,
                        title = assignment.Title,
                        description = assignment.Description,
                        due_date = assignment.DueDate,
                        points = assignment.Points,
                        assignment_type = assignment.AssignmentType,
                        curso_id = assignment.CursoId,
                        created_at = assignment.CreatedAt,
                        curso_nombre = courseName
                    },
                    completions = completionStats
                });
            }
        }
        
        Console.WriteLine($"✅ Tareas encontradas: {result.Count}");
        return Results.Ok(new { 
            assignments = result,
            userRole = user.Rol,
            total = result.Count 
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error obteniendo tareas: {ex.Message}");
        Console.WriteLine($"💥 Stack trace: {ex.StackTrace}");
        return Results.Problem("Error interno del servidor");
    }
}).RequireAuthorization();
// 🔹 CREAR TAREA
app.MapPost("/api/courses/{courseId}/assignments", async (HttpContext context, int courseId, [FromBody] AssignmentRequest request, [FromServices] Client supabase) =>
{
    Console.WriteLine($"➕ Endpoint: Crear tarea en curso {courseId}");
    
    try
    {
        var email = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
        if (string.IsNullOrEmpty(email))
        {
            Console.WriteLine("❌ Usuario no autenticado");
            return Results.Unauthorized();
        }

        Console.WriteLine($"📦 Datos recibidos de: {email}");
        Console.WriteLine($"📦 Request: Title='{request.Title}', CourseId={request.CursoId}, Type='{request.AssignmentType}'");

        var user = await GetUserFromToken(email);
        if (user == null) 
        {
            Console.WriteLine("❌ Usuario no encontrado");
            return Results.NotFound("Usuario no encontrado");
        }

        if (user.Rol != "Profesor")
        {
            Console.WriteLine("❌ Usuario no es profesor");
            return Results.Problem("Solo los profesores pueden crear tareas");
        }

        var tenant = GetTenantFromEmail(email);
        if (tenant == "unknown") 
        {
            Console.WriteLine("❌ Tenant no identificado");
            return Results.BadRequest("Tenant no identificado");
        }

        Console.WriteLine($"👤 Profesor: ID={user.Id}, Tenant={tenant}");

        // Verificar que el profesor está asignado al curso
        var hasAccess = await IsUserEnrolledInCourse(supabase, user.Id, courseId, tenant, user.Rol);
        Console.WriteLine($"🔐 Profesor asignado al curso: {hasAccess}");
        
        if (!hasAccess)
        {
            Console.WriteLine("❌ Profesor no asignado al curso");
            return Results.Problem("No estás asignado a este curso");
        }

        // 🔥 CREAR TAREA
        Console.WriteLine("🔥 Iniciando creación de tarea...");
        var assignmentId = await CreateAssignment(supabase, request, user.Id, tenant);
        
        if (assignmentId > 0)
        {
            Console.WriteLine($"✅ Tarea creada exitosamente con ID: {assignmentId}");
            return Results.Ok(new { 
                success = true, 
                assignmentId = assignmentId,
                message = "Tarea creada correctamente"
            });
        }

        Console.WriteLine("❌ Error al crear la tarea - assignmentId = 0");
        return Results.Problem("Error al crear la tarea en la base de datos");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error en endpoint crear tarea: {ex.Message}");
        Console.WriteLine($"💥 Stack trace: {ex.StackTrace}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"💥 Inner exception: {ex.InnerException.Message}");
        }
        return Results.Problem("Error interno del servidor");
    }
}).RequireAuthorization();
// 🔹 OBTENER CURSOS DEL USUARIO
app.MapGet("/api/my-courses", async (HttpContext context, [FromServices] Client supabase) =>
{
    Console.WriteLine("📚 Endpoint: Obtener cursos del usuario");
    try
    {
        var email = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
        if (string.IsNullOrEmpty(email))
        {
            Console.WriteLine("❌ Usuario no autenticado - Claim email no encontrado");
            return Results.Unauthorized();
        }

        Console.WriteLine($"🎯 Usuario autenticado: {email}");

        var user = await GetUserFromToken(email);
        if (user == null) 
        {
            Console.WriteLine("❌ Usuario no encontrado en base de datos");
            return Results.NotFound("Usuario no encontrado");
        }

        var tenant = GetTenantFromEmail(email);
        if (tenant == "unknown")
        {
            Console.WriteLine("❌ Tenant no identificado");
            return Results.BadRequest("Tenant no identificado");
        }

        Console.WriteLine($"👤 Procesando: UserId={user.Id}, Rol={user.Rol}, Tenant={tenant}");

        List<CourseInfo> courses;
        if (user.Rol == "Profesor")
        {
            Console.WriteLine("👨‍🏫 Obteniendo cursos del profesor...");
            courses = await GetProfessorCourses(supabase, user.Id, tenant);
        }
        else
        {
            Console.WriteLine("👨‍🎓 Obteniendo cursos del estudiante...");
            courses = await GetStudentCourses(supabase, user.Id, tenant);
        }

        Console.WriteLine($"✅ Retornando {courses.Count} cursos para {email}");
        return Results.Ok(new { 
            cursos = courses,
            userRole = user.Rol
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error obteniendo cursos: {ex.Message}");
        Console.WriteLine($"💥 Stack trace: {ex.StackTrace}");
        return Results.Problem("Error interno del servidor");
    }
}).RequireAuthorization();
// 🔹 ENDPOINTS DE DIAGNÓSTICO
app.MapGet("/api/debug/supabase", async ([FromServices] Client supabase) =>
{
    try
    {
        Console.WriteLine("🔍 Probando conexión a Supabase...");
        
        if (supabase == null)
        {
            return Results.Problem("Cliente Supabase es null");
        }

        var responseUcb = await supabase.From<AssignmentUcb>().Limit(1).Get();
        var responseUpb = await supabase.From<AssignmentUpb>().Limit(1).Get();
        var responseGmail = await supabase.From<AssignmentGmail>().Limit(1).Get();
        
        return Results.Ok(new { 
            status = "Conectado",
            ucb_count = responseUcb.Models?.Count ?? 0,
            upb_count = responseUpb.Models?.Count ?? 0,
            gmail_count = responseGmail.Models?.Count ?? 0
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error en diagnóstico: {ex.Message}");
        return Results.Problem($"Error: {ex.Message}");
    }
});

app.MapGet("/api/debug/assignments-detailed", async ([FromServices] Client supabase) =>
{
    try
    {
        Console.WriteLine("🔍 Diagnóstico detallado de tareas...");
        
        var assignmentsUcb = await supabase.From<AssignmentUcb>().Get();
        var assignmentsUpb = await supabase.From<AssignmentUpb>().Get();
        var assignmentsGmail = await supabase.From<AssignmentGmail>().Get();
        
        // Verificar estructura de tablas
        var sampleUcb = assignmentsUcb.Models.FirstOrDefault();
        var sampleUpb = assignmentsUpb.Models.FirstOrDefault();
        var sampleGmail = assignmentsGmail.Models.FirstOrDefault();
        
        return Results.Ok(new {
            status = "Conectado",
            tables = new {
                ucb = new {
                    count = assignmentsUcb.Models.Count,
                    columns = sampleUcb != null ? GetColumnNames(sampleUcb) : new List<string> { "No data" }
                },
                upb = new {
                    count = assignmentsUpb.Models.Count,
                    columns = sampleUpb != null ? GetColumnNames(sampleUpb) : new List<string> { "No data" }
                },
                gmail = new {
                    count = assignmentsGmail.Models.Count,
                    columns = sampleGmail != null ? GetColumnNames(sampleGmail) : new List<string> { "No data" }
                }
            }
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error en diagnóstico detallado: {ex.Message}");
        return Results.Problem($"Error: {ex.Message}");
    }
});

app.MapGet("/api/debug/courses/{courseId}/assignments", async (HttpContext context, int courseId, [FromServices] Client supabase) =>
{
    Console.WriteLine($"🔍 DIAGNÓSTICO TAREAS CURSO {courseId}");
    
    try
    {
        var email = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
        if (string.IsNullOrEmpty(email))
        {
            return Results.Ok(new { error = "No autenticado" });
        }

        var user = await GetUserFromToken(email);
        var tenant = GetTenantFromEmail(email);

        Console.WriteLine($"👤 Usuario: {user?.Id}, Rol: {user?.Rol}, Tenant: {tenant}");

        // 1. Verificar acceso al curso
        var hasAccess = user != null ? await IsUserEnrolledInCourse(supabase, user.Id, courseId, tenant, user.Rol) : false;
        Console.WriteLine($"🔐 Tiene acceso al curso: {hasAccess}");

        // 2. Obtener tareas directamente
        var assignments = await GetCourseAssignments(supabase, courseId, tenant);
        Console.WriteLine($"📝 Tareas encontradas: {assignments.Count}");

        // 3. Verificar curso existe
        var courseName = await GetCourseName(supabase, courseId, tenant);
        Console.WriteLine($"📚 Curso: {courseName} (ID: {courseId})");

        return Results.Ok(new
        {
            user_info = new { id = user?.Id, rol = user?.Rol, email = email },
            course_info = new { id = courseId, name = courseName, exists = courseName != null },
            access_info = new { has_access = hasAccess },
            assignments_info = new
            {
                count = assignments.Count,
                assignments = assignments.Select(a => new
                {
                    id = a.Id,
                    title = a.Title,
                    course_id = a.CursoId,
                    type = a.AssignmentType,
                    due_date = a.DueDate
                })
            },
            tenant_info = tenant
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error en diagnóstico: {ex.Message}");
        return Results.Problem($"Error: {ex.Message}");
    }
}).RequireAuthorization();

app.MapGet("/api/debug/full-diagnostics", async (HttpContext context, [FromServices] Client supabase) =>
{
    Console.WriteLine("🔍 ========== DIAGNÓSTICO COMPLETO ==========");
    
    try
    {
        var email = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
        if (string.IsNullOrEmpty(email))
        {
            return Results.Ok(new { error = "No autenticado", step = "auth" });
        }

        Console.WriteLine($"👤 Usuario autenticado: {email}");
        
        var diagnostics = new List<object>();
        
        // 1. Diagnóstico de tenant
        var tenant = GetTenantFromEmail(email);
        diagnostics.Add(new { step = "tenant", email = email, tenant = tenant });
        
        // 2. Diagnóstico de usuario
        var user = await GetUserFromToken(email);
        diagnostics.Add(new { 
            step = "user", 
            found = user != null, 
            userId = user?.Id, 
            userRole = user?.Rol 
        });

        if (user == null)
        {
            return Results.Ok(new { 
                status = "Usuario no encontrado", 
                diagnostics = diagnostics 
            });
        }

        // 3. Diagnóstico de cursos
        List<CourseInfo> courses;
        if (user.Rol == "Profesor")
        {
            courses = await GetProfessorCourses(supabase, user.Id, tenant);
        }
        else
        {
            courses = await GetStudentCourses(supabase, user.Id, tenant);
        }
        
        diagnostics.Add(new { 
            step = "courses", 
            count = courses.Count,
            courses = courses.Select(c => new { c.Id, c.Nombre, c.Codigo })
        });

        // 4. Diagnóstico de tareas por curso
        var assignmentsByCourse = new List<object>();
        foreach (var course in courses.Take(3)) // Solo primeros 3 cursos para no saturar
        {
            var assignments = await GetCourseAssignments(supabase, course.Id, tenant);
            assignmentsByCourse.Add(new {
                courseId = course.Id,
                courseName = course.Nombre,
                assignmentsCount = assignments.Count,
                assignments = assignments.Select(a => new { a.Id, a.Title, a.CursoId })
            });
        }
        
        diagnostics.Add(new { 
            step = "assignments_by_course",
            data = assignmentsByCourse
        });

        // 5. Diagnóstico de tablas de assignments
        var tableTests = new List<object>();
        
        // Probar cada tabla de assignments
        try
        {
            var testUcb = await supabase.From<AssignmentUcb>()
                .Where(x => x.IsActive == true)
                .Limit(2)
                .Get();
            tableTests.Add(new {
                table = "tenant_ucb_assignments",
                success = true,
                count = testUcb.Models.Count,
                sample = testUcb.Models.Select(a => new { a.Id, a.Title, a.CursoId })
            });
        }
        catch (Exception ex)
        {
            tableTests.Add(new {
                table = "tenant_ucb_assignments", 
                success = false, 
                error = ex.Message 
            });
        }

        try
        {
            var testUpb = await supabase.From<AssignmentUpb>()
                .Where(x => x.IsActive == true)
                .Limit(2)
                .Get();
            tableTests.Add(new {
                table = "tenant_upb_assignments",
                success = true,
                count = testUpb.Models.Count,
                sample = testUpb.Models.Select(a => new { a.Id, a.Title, a.CursoId })
            });
        }
        catch (Exception ex)
        {
            tableTests.Add(new {
                table = "tenant_upb_assignments", 
                success = false, 
                error = ex.Message 
            });
        }

        try
        {
            var testGmail = await supabase.From<AssignmentGmail>()
                .Where(x => x.IsActive == true)
                .Limit(2)
                .Get();
            tableTests.Add(new {
                table = "tenant_gmail_assignments",
                success = true,
                count = testGmail.Models.Count,
                sample = testGmail.Models.Select(a => new { a.Id, a.Title, a.CursoId })
            });
        }
        catch (Exception ex)
        {
            tableTests.Add(new {
                table = "tenant_gmail_assignments", 
                success = false, 
                error = ex.Message 
            });
        }

        diagnostics.Add(new { 
            step = "table_tests",
            tables = tableTests
        });

        return Results.Ok(new {
            status = "Diagnóstico completado",
            user_info = new { id = user.Id, rol = user.Rol, email = email, tenant = tenant },
            diagnostics = diagnostics
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error en diagnóstico completo: {ex.Message}");
        return Results.Problem($"Error: {ex.Message}");
    }
}).RequireAuthorization();
// 🔹 COMPLETAR TAREA CON ARCHIVOS
app.MapPost("/api/assignments/{assignmentId}/complete", async (
    HttpContext context,
    int assignmentId,
    [FromServices] Client supabase
) =>
{
    Console.WriteLine($"📤 Endpoint: Completar tarea {assignmentId} con archivos");
    
    try
    {
        var email = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
        if (string.IsNullOrEmpty(email))
        {
            Console.WriteLine("❌ Usuario no autenticado");
            return Results.Unauthorized();
        }

        var user = await GetUserFromToken(email);
        if (user == null)
        {
            Console.WriteLine("❌ Usuario no encontrado");
            return Results.NotFound("Usuario no encontrado");
        }

        var tenant = GetTenantFromEmail(email);
        var form = await context.Request.ReadFormAsync();
        var files = form.Files;
        var notes = form["notes"].ToString();

        Console.WriteLine($"📦 Usuario: {email}, Archivos: {files.Count}");

        // Validar que el estudiante esté inscrito en el curso
        bool isEnrolled = false;
        int courseId = 0;

        if (tenant == "ucb")
        {
            var assignment = await supabase.From<AssignmentUcb>()
                .Where(x => x.Id == assignmentId)
                .Single();
            
            if (assignment == null)
            {
                Console.WriteLine("❌ Tarea no encontrada");
                return Results.NotFound("Tarea no encontrada");
            }

            courseId = assignment.CursoId;

            var enrollment = await supabase.From<InscripcionUcb>()
                .Where(x => x.UsuarioId == user.Id && x.CursoId == courseId)
                .Get();
            
            isEnrolled = enrollment.Models.Any();
        }
        else if (tenant == "upb")
        {
            var assignment = await supabase.From<AssignmentUpb>()
                .Where(x => x.Id == assignmentId)
                .Single();
            
            if (assignment == null)
            {
                Console.WriteLine("❌ Tarea no encontrada");
                return Results.NotFound("Tarea no encontrada");
            }

            courseId = assignment.CursoId;

            var enrollment = await supabase.From<InscripcionUpb>()
                .Where(x => x.UsuarioId == user.Id && x.CursoId == courseId)
                .Get();
            
            isEnrolled = enrollment.Models.Any();
        }
        else // gmail
        {
            var assignment = await supabase.From<AssignmentGmail>()
                .Where(x => x.Id == assignmentId)
                .Single();
            
            if (assignment == null)
            {
                Console.WriteLine("❌ Tarea no encontrada");
                return Results.NotFound("Tarea no encontrada");
            }

            courseId = assignment.CursoId;

            var enrollment = await supabase.From<InscripcionGmail>()
                .Where(x => x.UsuarioId == user.Id && x.CursoId == courseId)
                .Get();
            
            isEnrolled = enrollment.Models.Any();
        }

        if (!isEnrolled)
        {
            Console.WriteLine("❌ Estudiante no inscrito en el curso");
            return Results.Problem("No estás inscrito en este curso");
        }

        // Procesar archivos
        var fileResults = new List<object>();
        var codeAnalysisResults = new List<object>();
        var codeFileExtensions = new[] { ".py", ".js", ".ts", ".jsx", ".tsx", ".java", ".cpp", ".c", ".cs", ".html", ".css" };

        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("http://localhost:5015");

        foreach (var file in files)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var isCodeFile = codeFileExtensions.Contains(extension);

            Console.WriteLine($"📄 Procesando archivo: {file.FileName} (Código: {isCodeFile}, Tamaño: {file.Length} bytes)");

            // Guardar archivo en la base de datos
            try
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                if (tenant == "ucb")
                {
                    var fileRecord = new AssignmentFileUcb
                    {
                        AssignmentId = assignmentId,
                        StudentId = user.Id,
                        FileName = file.FileName,
                        FileContent = fileBytes,
                        FileSize = file.Length,
                        ContentType = file.ContentType ?? "application/octet-stream",
                        IsCodeFile = isCodeFile,
                        UploadedAt = DateTime.UtcNow
                    };
                    await supabase.From<AssignmentFileUcb>().Insert(fileRecord);
                }
                else if (tenant == "upb")
                {
                    var fileRecord = new AssignmentFileUpb
                    {
                        AssignmentId = assignmentId,
                        StudentId = user.Id,
                        FileName = file.FileName,
                        FileContent = fileBytes,
                        FileSize = file.Length,
                        ContentType = file.ContentType ?? "application/octet-stream",
                        IsCodeFile = isCodeFile,
                        UploadedAt = DateTime.UtcNow
                    };
                    await supabase.From<AssignmentFileUpb>().Insert(fileRecord);
                }
                else // gmail
                {
                    var fileRecord = new AssignmentFileGmail
                    {
                        AssignmentId = assignmentId,
                        StudentId = user.Id,
                        FileName = file.FileName,
                        FileContent = fileBytes,
                        FileSize = file.Length,
                        ContentType = file.ContentType ?? "application/octet-stream",
                        IsCodeFile = isCodeFile,
                        UploadedAt = DateTime.UtcNow
                    };
                    await supabase.From<AssignmentFileGmail>().Insert(fileRecord);
                }

                Console.WriteLine($"💾 Archivo guardado en base de datos: {file.FileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Error guardando archivo {file.FileName}: {ex.Message}");
            }

            // Si es archivo de código, enviar al microservicio de análisis
            if (isCodeFile && file.Length > 0)
            {
                try
                {
                    // Resetear el stream del archivo
                    using var content = new MultipartFormDataContent();
                    using var fileStream = file.OpenReadStream();
                    using var streamContent = new StreamContent(fileStream);
                    
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
                    content.Add(streamContent, "file", file.FileName);
                    content.Add(new StringContent(assignmentId.ToString()), "assignment_id");
                    content.Add(new StringContent(user.Id.ToString()), "student_id");

                    var response = await httpClient.PostAsync("/api/submissions/upload", content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"✅ Análisis completado para {file.FileName}");
                        codeAnalysisResults.Add(new
                        {
                            fileName = file.FileName,
                            status = "analyzed",
                            response = responseContent
                        });
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"⚠️ Error analizando {file.FileName}: {errorContent}");
                        codeAnalysisResults.Add(new
                        {
                            fileName = file.FileName,
                            status = "error",
                            error = errorContent
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"💥 Error procesando {file.FileName}: {ex.Message}");
                    codeAnalysisResults.Add(new
                    {
                        fileName = file.FileName,
                        status = "error",
                        error = ex.Message
                    });
                }
            }

            fileResults.Add(new
            {
                fileName = file.FileName,
                size = file.Length,
                isCode = isCodeFile,
                extension = extension
            });
        }

        // Crear o actualizar registro de completación
        var completedAt = DateTime.UtcNow;
        var status = codeAnalysisResults.Any(r => r.GetType().GetProperty("status")?.GetValue(r)?.ToString() == "error") 
            ? "completed_with_errors" 
            : "completed";

        if (tenant == "ucb")
        {
            var existing = await supabase.From<AssignmentCompletionUcb>()
                .Where(x => x.AssignmentId == assignmentId && x.StudentId == user.Id)
                .Get();

            if (existing.Models.Any())
            {
                var completion = existing.Models.First();
                completion.CompletedAt = completedAt;
                completion.Status = status;
                completion.SubmittedContent = notes;
                await supabase.From<AssignmentCompletionUcb>().Update(completion);
            }
            else
            {
                var newCompletion = new AssignmentCompletionUcb
                {
                    AssignmentId = assignmentId,
                    StudentId = user.Id,
                    CompletedAt = completedAt,
                    Status = status,
                    SubmittedContent = notes
                };
                await supabase.From<AssignmentCompletionUcb>().Insert(newCompletion);
            }
        }
        else if (tenant == "upb")
        {
            var existing = await supabase.From<AssignmentCompletionUpb>()
                .Where(x => x.AssignmentId == assignmentId && x.StudentId == user.Id)
                .Get();

            if (existing.Models.Any())
            {
                var completion = existing.Models.First();
                completion.CompletedAt = completedAt;
                completion.Status = status;
                completion.SubmittedContent = notes;
                await supabase.From<AssignmentCompletionUpb>().Update(completion);
            }
            else
            {
                var newCompletion = new AssignmentCompletionUpb
                {
                    AssignmentId = assignmentId,
                    StudentId = user.Id,
                    CompletedAt = completedAt,
                    Status = status,
                    SubmittedContent = notes
                };
                await supabase.From<AssignmentCompletionUpb>().Insert(newCompletion);
            }
        }
        else // gmail
        {
            var existing = await supabase.From<AssignmentCompletionGmail>()
                .Where(x => x.AssignmentId == assignmentId && x.StudentId == user.Id)
                .Get();

            if (existing.Models.Any())
            {
                var completion = existing.Models.First();
                completion.CompletedAt = completedAt;
                completion.Status = status;
                completion.SubmittedContent = notes;
                await supabase.From<AssignmentCompletionGmail>().Update(completion);
            }
            else
            {
                var newCompletion = new AssignmentCompletionGmail
                {
                    AssignmentId = assignmentId,
                    StudentId = user.Id,
                    CompletedAt = completedAt,
                    Status = status,
                    SubmittedContent = notes
                };
                await supabase.From<AssignmentCompletionGmail>().Insert(newCompletion);
            }
        }

        Console.WriteLine($"✅ Tarea completada. Archivos: {files.Count}, Código analizado: {codeAnalysisResults.Count}");

        return Results.Ok(new
        {
            message = "Tarea completada exitosamente",
            filesProcessed = files.Count,
            codeFilesAnalyzed = codeAnalysisResults.Count,
            files = fileResults,
            codeAnalysis = codeAnalysisResults,
            status = status
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error completando tarea: {ex.Message}");
        Console.WriteLine($"💥 Stack trace: {ex.StackTrace}");
        return Results.Problem($"Error completando tarea: {ex.Message}");
    }
}).RequireAuthorization()
  .DisableAntiforgery(); // Necesario para file uploads

// 🔹 DESCARGAR ARCHIVO DE TAREA
app.MapGet("/api/assignments/{assignmentId}/files/{fileId}", async (
    HttpContext context,
    int assignmentId,
    int fileId,
    [FromServices] Client supabase
) =>
{
    Console.WriteLine($"📥 Endpoint: Descargar archivo {fileId} de tarea {assignmentId}");
    
    try
    {
        var email = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
        if (string.IsNullOrEmpty(email))
        {
            Console.WriteLine("❌ Usuario no autenticado");
            return Results.Unauthorized();
        }

        var user = await GetUserFromToken(email);
        if (user == null)
        {
            Console.WriteLine("❌ Usuario no encontrado");
            return Results.NotFound("Usuario no encontrado");
        }

        var tenant = GetTenantFromEmail(email);
        
        // Buscar el archivo según el tenant
        if (tenant == "ucb")
        {
            var fileRecord = await supabase.From<AssignmentFileUcb>()
                .Where(x => x.Id == fileId && x.AssignmentId == assignmentId)
                .Single();

            if (fileRecord == null)
            {
                Console.WriteLine("❌ Archivo no encontrado");
                return Results.NotFound("Archivo no encontrado");
            }

            // Verificar permisos: solo el estudiante que subió el archivo o profesores pueden descargarlo
            if (user.Rol == "Estudiante" && fileRecord.StudentId != user.Id)
            {
                Console.WriteLine("❌ Sin permiso para descargar este archivo");
                return Results.Forbid();
            }

            Console.WriteLine($"✅ Descargando: {fileRecord.FileName} ({fileRecord.FileSize} bytes)");
            return Results.File(fileRecord.FileContent, fileRecord.ContentType, fileRecord.FileName);
        }
        else if (tenant == "upb")
        {
            var fileRecord = await supabase.From<AssignmentFileUpb>()
                .Where(x => x.Id == fileId && x.AssignmentId == assignmentId)
                .Single();

            if (fileRecord == null)
            {
                Console.WriteLine("❌ Archivo no encontrado");
                return Results.NotFound("Archivo no encontrado");
            }

            if (user.Rol == "Estudiante" && fileRecord.StudentId != user.Id)
            {
                Console.WriteLine("❌ Sin permiso para descargar este archivo");
                return Results.Forbid();
            }

            Console.WriteLine($"✅ Descargando: {fileRecord.FileName} ({fileRecord.FileSize} bytes)");
            return Results.File(fileRecord.FileContent, fileRecord.ContentType, fileRecord.FileName);
        }
        else // gmail
        {
            var fileRecord = await supabase.From<AssignmentFileGmail>()
                .Where(x => x.Id == fileId && x.AssignmentId == assignmentId)
                .Single();

            if (fileRecord == null)
            {
                Console.WriteLine("❌ Archivo no encontrado");
                return Results.NotFound("Archivo no encontrado");
            }

            if (user.Rol == "Estudiante" && fileRecord.StudentId != user.Id)
            {
                Console.WriteLine("❌ Sin permiso para descargar este archivo");
                return Results.Forbid();
            }

            Console.WriteLine($"✅ Descargando: {fileRecord.FileName} ({fileRecord.FileSize} bytes)");
            return Results.File(fileRecord.FileContent, fileRecord.ContentType, fileRecord.FileName);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error descargando archivo: {ex.Message}");
        return Results.Problem($"Error descargando archivo: {ex.Message}");
    }
}).RequireAuthorization();

// 🔹 LISTAR ARCHIVOS DE UNA ENTREGA
app.MapGet("/api/assignments/{assignmentId}/files", async (
    HttpContext context,
    int assignmentId,
    [FromQuery] int? studentId,
    [FromServices] Client supabase
) =>
{
    Console.WriteLine($"📋 Endpoint: Listar archivos de tarea {assignmentId}");
    
    try
    {
        var email = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
        if (string.IsNullOrEmpty(email))
        {
            Console.WriteLine("❌ Usuario no autenticado");
            return Results.Unauthorized();
        }

        var user = await GetUserFromToken(email);
        if (user == null)
        {
            Console.WriteLine("❌ Usuario no encontrado");
            return Results.NotFound("Usuario no encontrado");
        }

        var tenant = GetTenantFromEmail(email);
        var targetStudentId = studentId ?? user.Id;

        // Si es estudiante, solo puede ver sus propios archivos
        if (user.Rol == "Estudiante" && targetStudentId != user.Id)
        {
            Console.WriteLine("❌ Sin permiso para ver archivos de otros estudiantes");
            return Results.Forbid();
        }

        var files = new List<object>();

        if (tenant == "ucb")
        {
            var fileRecords = await supabase.From<AssignmentFileUcb>()
                .Where(x => x.AssignmentId == assignmentId && x.StudentId == targetStudentId)
                .Get();

            files = fileRecords.Models.Select(f => new
            {
                id = f.Id,
                fileName = f.FileName,
                fileSize = f.FileSize,
                contentType = f.ContentType,
                isCodeFile = f.IsCodeFile,
                uploadedAt = f.UploadedAt,
                downloadUrl = $"/api/assignments/{assignmentId}/files/{f.Id}"
            }).ToList<object>();
        }
        else if (tenant == "upb")
        {
            var fileRecords = await supabase.From<AssignmentFileUpb>()
                .Where(x => x.AssignmentId == assignmentId && x.StudentId == targetStudentId)
                .Get();

            files = fileRecords.Models.Select(f => new
            {
                id = f.Id,
                fileName = f.FileName,
                fileSize = f.FileSize,
                contentType = f.ContentType,
                isCodeFile = f.IsCodeFile,
                uploadedAt = f.UploadedAt,
                downloadUrl = $"/api/assignments/{assignmentId}/files/{f.Id}"
            }).ToList<object>();
        }
        else // gmail
        {
            var fileRecords = await supabase.From<AssignmentFileGmail>()
                .Where(x => x.AssignmentId == assignmentId && x.StudentId == targetStudentId)
                .Get();

            files = fileRecords.Models.Select(f => new
            {
                id = f.Id,
                fileName = f.FileName,
                fileSize = f.FileSize,
                contentType = f.ContentType,
                isCodeFile = f.IsCodeFile,
                uploadedAt = f.UploadedAt,
                downloadUrl = $"/api/assignments/{assignmentId}/files/{f.Id}"
            }).ToList<object>();
        }

        Console.WriteLine($"✅ Encontrados {files.Count} archivos");
        return Results.Ok(new { files = files, total = files.Count });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error listando archivos: {ex.Message}");
        return Results.Problem($"Error listando archivos: {ex.Message}");
    }
}).RequireAuthorization();

// Método helper para obtener nombres de columnas
static List<string> GetColumnNames(object obj)
{
    return obj.GetType().GetProperties().Select(p => p.Name).ToList();
}

app.Run();
// ═══════════════════════════════════════════════════════════════
// 🔹 MODELOS Y RECORDS
// ═══════════════════════════════════════════════════════════════

public record AssignmentRequest(
    string Title,
    string Description,
    DateTime? DueDate,
    decimal? Points,
    string AssignmentType,
    int CursoId,
    string CreatedBy
);

public class AssignmentCompletionRequest
{
    public string? SubmittedContent { get; set; }
}

public record UserInfo(int Id, string Rol);
public record AssignmentInfo(
    int Id, 
    string Title, 
    string Description, 
    DateTime? DueDate, 
    decimal? Points, 
    string AssignmentType, 
    int CursoId, 
    DateTime CreatedAt
);
public record AssignmentWithCourseInfo(
    AssignmentInfo Assignment,
    string CursoNombre,
    string CursoCodigo
);
public record CourseInfo(int Id, string Nombre, string Codigo);
public record CompletionInfo(DateTime? CompletedAt, string Status, string? SubmittedContent);
public record CompletionStats(int Total, int Completed);


// ═══════════════════════════════════════════════════════════════
// 🔹 MODELOS SUPABASE
// ═══════════════════════════════════════════════════════════════

// Modelos para UCB
[Table("tenant_ucb_usuarios")]
public class UsuarioUcb : BaseModel
{
    [PrimaryKey("id", true)]
    public int Id { get; set; }
    
    [Column("nombre")]
    public string? Nombre { get; set; }
    
    [Column("apellido")]
    public string? Apellido { get; set; }
    
    [Column("email")]
    public string? Email { get; set; }
    
    [Column("rol")]
    public string? Rol { get; set; }
}

[Table("tenant_ucb_cursos")]
public class CursoUcb : BaseModel
{
    [PrimaryKey("id", true)]
    public int Id { get; set; }
    
    [Column("nombre")]
    public string? Nombre { get; set; }
    
    [Column("codigo")]
    public string? Codigo { get; set; }
    
    [Column("profesor_id")]
    public int? ProfesorId { get; set; }
}

[Table("tenant_ucb_assignments")]
public class AssignmentUcb : BaseModel
{
    [PrimaryKey("id", true)]
    public int Id { get; set; }
    
    [Column("title")]
    public string Title { get; set; } = string.Empty;
    
    [Column("description")]
    public string Description { get; set; } = string.Empty;
    
    [Column("due_date")]
    public DateTime? DueDate { get; set; }
    
    [Column("points")]
    public decimal? Points { get; set; }
    
    [Column("assignment_type")]
    public string AssignmentType { get; set; } = "tarea";
    
    [Column("curso_id")]
    public int CursoId { get; set; }
    
    [Column("profesor_id")]
    public int ProfesorId { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
    
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    [Column("status")]
    public string Status { get; set; } = "active";
}

[Table("tenant_ucb_assignment_completions")]
public class AssignmentCompletionUcb : BaseModel
{
    [PrimaryKey("id", true)]
    public int Id { get; set; }
    
    [Column("assignment_id")]
    public int AssignmentId { get; set; }
    
    [Column("student_id")]
    public int StudentId { get; set; }
    
    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }
    
    [Column("status")]
    public string Status { get; set; } = "pending";
    
    [Column("submitted_content")]
    public string? SubmittedContent { get; set; }
    
    [Column("grade")]
    public decimal? Grade { get; set; }
    
    [Column("feedback")]
    public string? Feedback { get; set; }
}

[Table("tenant_ucb_inscripciones")]
public class InscripcionUcb : BaseModel
{
    [PrimaryKey("id", true)]
    public int Id { get; set; }
    
    [Column("usuario_id")]
    public int UsuarioId { get; set; }
    
    [Column("curso_id")]
    public int CursoId { get; set; }
}

// Modelos para UPB
[Table("tenant_upb_usuarios")]
public class UsuarioUpb : BaseModel
{
    [PrimaryKey("id", true)]
    public int Id { get; set; }
    
    [Column("nombre")]
    public string? Nombre { get; set; }
    
    [Column("apellido")]
    public string? Apellido { get; set; }
    
    [Column("email")]
    public string? Email { get; set; }
    
    [Column("rol")]
    public string? Rol { get; set; }
}

[Table("tenant_upb_cursos")]
public class CursoUpb : BaseModel
{
    [PrimaryKey("id", true)]
    public int Id { get; set; }
    
    [Column("nombre")]
    public string? Nombre { get; set; }
    
    [Column("codigo")]
    public string? Codigo { get; set; }
    
    [Column("profesor_id")]
    public int? ProfesorId { get; set; }
}

[Table("tenant_upb_assignments")]
public class AssignmentUpb : BaseModel
{
    [PrimaryKey("id", true)]
    public int Id { get; set; }
    
    [Column("title")]
    public string Title { get; set; } = string.Empty;
    
    [Column("description")]
    public string Description { get; set; } = string.Empty;
    
    [Column("due_date")]
    public DateTime? DueDate { get; set; }
    
    [Column("points")]
    public decimal? Points { get; set; }
    
    [Column("assignment_type")]
    public string AssignmentType { get; set; } = "tarea";
    
    [Column("curso_id")]
    public int CursoId { get; set; }
    
    [Column("profesor_id")]
    public int ProfesorId { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
    
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    [Column("status")]
    public string Status { get; set; } = "active";
}

[Table("tenant_upb_assignment_completions")]
public class AssignmentCompletionUpb : BaseModel
{
    [PrimaryKey("id", true)]
    public int Id { get; set; }
    
    [Column("assignment_id")]
    public int AssignmentId { get; set; }
    
    [Column("student_id")]
    public int StudentId { get; set; }
    
    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }
    
    [Column("status")]
    public string Status { get; set; } = "pending";
    
    [Column("submitted_content")]
    public string? SubmittedContent { get; set; }
    
    [Column("grade")]
    public decimal? Grade { get; set; }
    
    [Column("feedback")]
    public string? Feedback { get; set; }
}

[Table("tenant_upb_inscripciones")]
public class InscripcionUpb : BaseModel
{
    [PrimaryKey("id", true)]
    public int Id { get; set; }
    
    [Column("usuario_id")]
    public int UsuarioId { get; set; }
    
    [Column("curso_id")]
    public int CursoId { get; set; }
}

// Modelos para Gmail
[Table("tenant_gmail_usuarios")]
public class UsuarioGmail : BaseModel
{
    [PrimaryKey("id", true)]
    public int Id { get; set; }
    
    [Column("nombre")]
    public string? Nombre { get; set; }
    
    [Column("apellido")]
    public string? Apellido { get; set; }
    
    [Column("email")]
    public string? Email { get; set; }
    
    [Column("rol")]
    public string? Rol { get; set; }
}

[Table("tenant_gmail_cursos")]
public class CursoGmail : BaseModel
{
    [PrimaryKey("id", true)]
    public int Id { get; set; }
    
    [Column("nombre")]
    public string? Nombre { get; set; }
    
    [Column("codigo")]
    public string? Codigo { get; set; }
    
    [Column("profesor_id")]
    public int? ProfesorId { get; set; }
}

[Table("tenant_gmail_assignments")]
public class AssignmentGmail : BaseModel
{
    [PrimaryKey("id", true)]
    public int Id { get; set; }
    
    [Column("title")]
    public string Title { get; set; } = string.Empty;
    
    [Column("description")]
    public string Description { get; set; } = string.Empty;
    
    [Column("due_date")]
    public DateTime? DueDate { get; set; }

    [Column("points")]
    public decimal? Points { get; set; }
    
    [Column("assignment_type")]
    public string AssignmentType { get; set; } = "tarea";
    
    [Column("curso_id")]
    public int CursoId { get; set; }
    
    [Column("profesor_id")]
    public int ProfesorId { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
    
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    [Column("status")]
    public string Status { get; set; } = "active";
}

[Table("tenant_gmail_assignment_completions")]
public class AssignmentCompletionGmail : BaseModel
{
    [PrimaryKey("id", true)]
    public int Id { get; set; }
    
    [Column("assignment_id")]
    public int AssignmentId { get; set; }
    
    [Column("student_id")]
    public int StudentId { get; set; }
    
    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }
    
    [Column("status")]
    public string Status { get; set; } = "pending";
    
    [Column("submitted_content")]
    public string? SubmittedContent { get; set; }
    
    [Column("grade")]
    public decimal? Grade { get; set; }
    
    [Column("feedback")]
    public string? Feedback { get; set; }
}

[Table("tenant_gmail_inscripciones")]
public class InscripcionGmail : BaseModel
{
    [PrimaryKey("id", true)]
    public int Id { get; set; }
    
    [Column("usuario_id")]
    public int UsuarioId { get; set; }
    
    [Column("curso_id")]
    public int CursoId { get; set; }
}

// Modelos para archivos de tareas (multi-tenant)
[Table("tenant_ucb_assignment_files")]
public class AssignmentFileUcb : BaseModel
{
    [PrimaryKey("id", true)]
    public int Id { get; set; }
    
    [Column("assignment_id")]
    public int AssignmentId { get; set; }
    
    [Column("student_id")]
    public int StudentId { get; set; }
    
    [Column("file_name")]
    public string FileName { get; set; } = string.Empty;
    
    [Column("file_content")]
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    
    [Column("file_size")]
    public long FileSize { get; set; }
    
    [Column("content_type")]
    public string ContentType { get; set; } = string.Empty;
    
    [Column("is_code_file")]
    public bool IsCodeFile { get; set; }
    
    [Column("uploaded_at")]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

[Table("tenant_upb_assignment_files")]
public class AssignmentFileUpb : BaseModel
{
    [PrimaryKey("id", true)]
    public int Id { get; set; }
    
    [Column("assignment_id")]
    public int AssignmentId { get; set; }
    
    [Column("student_id")]
    public int StudentId { get; set; }
    
    [Column("file_name")]
    public string FileName { get; set; } = string.Empty;
    
    [Column("file_content")]
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    
    [Column("file_size")]
    public long FileSize { get; set; }
    
    [Column("content_type")]
    public string ContentType { get; set; } = string.Empty;
    
    [Column("is_code_file")]
    public bool IsCodeFile { get; set; }
    
    [Column("uploaded_at")]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

[Table("tenant_gmail_assignment_files")]
public class AssignmentFileGmail : BaseModel
{
    [PrimaryKey("id", true)]
    public int Id { get; set; }
    
    [Column("assignment_id")]
    public int AssignmentId { get; set; }
    
    [Column("student_id")]
    public int StudentId { get; set; }
    
    [Column("file_name")]
    public string FileName { get; set; } = string.Empty;
    
    [Column("file_content")]
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    
    [Column("file_size")]
    public long FileSize { get; set; }
    
    [Column("content_type")]
    public string ContentType { get; set; } = string.Empty;
    
    [Column("is_code_file")]
    public bool IsCodeFile { get; set; }
    
    [Column("uploaded_at")]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}