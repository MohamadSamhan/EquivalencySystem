// Request Equivalency Page (Student)
// Form to request equivalency between a student course and a target course

function RequestsPage({ onNavigate }) {
  const [studentCourses, setStudentCourses] = React.useState([]);
  const [targetCourses, setTargetCourses] = React.useState([]);
  const [selectedStudentCourse, setSelectedStudentCourse] = React.useState('');
  const [selectedTargetCourse, setSelectedTargetCourse] = React.useState('');
  const [loading, setLoading] = React.useState(false);
  const [loadingData, setLoadingData] = React.useState(true);
  const [success, setSuccess] = React.useState('');
  const [error, setError] = React.useState('');

  React.useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      const [studentRes, coursesRes] = await Promise.all([
        studentCoursesAPI.getAll().catch(() => ({ data: [] })),
        coursesAPI.getAll().catch(() => ({ data: [] })),
      ]);
      setStudentCourses(Array.isArray(studentRes.data) ? studentRes.data : []);
      setTargetCourses(Array.isArray(coursesRes.data) ? coursesRes.data : []);
    } catch (err) {
      console.error('Error loading courses:', err);
    } finally {
      setLoadingData(false);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setSuccess('');

    if (!selectedStudentCourse || !selectedTargetCourse) {
      setError('يرجى اختيار كلا المساقين.');
      return;
    }

    setLoading(true);
    try {
      await requestsAPI.submit({
        studentCourseId: parseInt(selectedStudentCourse),
        targetCourseId: parseInt(selectedTargetCourse),
      });

      setSuccess('تم تقديم طلب المعادلة بنجاح! الحالة: قيد الانتظار 🎉');
      setSelectedStudentCourse('');
      setSelectedTargetCourse('');
      setTimeout(() => setSuccess(''), 5000);
    } catch (err) {
      setError(err.response?.data?.message || 'فشل في تقديم الطلب. يرجى المحاولة مرة أخرى.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="main-content" dir="rtl">
      <div className="page-header">
        <h1 className="page-title">طلب معادلة</h1>
        <p className="page-subtitle">قدم طلباً لمعادلة مساق من جامعتك السابقة بمساق في هذه الجامعة.</p>
      </div>

      {success && (
        <div className="alert alert-success">
          <span>✅</span> {success}
        </div>
      )}

      {error && (
        <div className="alert alert-error">
          <span>⚠️</span> {error}
        </div>
      )}

      <div className="request-layout">
        {/* Main Form Card */}
        <div className="form-card" style={{ maxWidth: '100%' }}>
          <h2 className="card-title">نموذج طلب المعادلة</h2>

          {loadingData ? (
            <div className="loading-screen" style={{ minHeight: 200 }}>
              <div className="spinner"></div>
              <span>جاري تحميل المساقات...</span>
            </div>
          ) : (
            <form onSubmit={handleSubmit}>
              <div className="form-group">
                <label className="form-label" htmlFor="student-course-select">
                  مساقك (من الجامعة السابقة) <span style={{ color: 'var(--danger-500)' }}>*</span>
                </label>
                <select
                  id="student-course-select"
                  value={selectedStudentCourse}
                  onChange={(e) => setSelectedStudentCourse(e.target.value)}
                >
                  <option value="">اختر مساقك</option>
                  {studentCourses.map((course) => (
                    <option key={course.id} value={course.id}>
                      {course.courseName} ({course.creditHours} ساعات)
                    </option>
                  ))}
                </select>
                {studentCourses.length === 0 && (
                  <p className="course-hint">
                    ⚠️ لا توجد مساقات مضافة. يرجى{' '}
                    <a onClick={() => onNavigate('add-course')}>إضافة مساق</a>{' '}
                    أولاً.
                  </p>
                )}
              </div>

              <div className="form-group">
                <label className="form-label" htmlFor="target-course-select">
                  المساق المستهدف في جامعتنا <span style={{ color: 'var(--danger-500)' }}>*</span>
                </label>
                <select
                  id="target-course-select"
                  value={selectedTargetCourse}
                  onChange={(e) => setSelectedTargetCourse(e.target.value)}
                >
                  <option value="">اختر المساق المستهدف</option>
                  {targetCourses.map((course) => (
                    <option key={course.id} value={course.id}>
                      {course.courseName} ({course.creditHours} ساعات)
                    </option>
                  ))}
                </select>
              </div>

              {/* Course Preview Box */}
              {selectedStudentCourse && selectedTargetCourse && (
                <div className="course-preview-box">
                  <div className="preview-label">🔍 معاينة الطلب</div>
                  <div className="preview-courses">
                    <div className="preview-course-chip">
                      {studentCourses.find(c => c.id == selectedStudentCourse)?.courseName}
                    </div>
                    <div className="preview-arrow">←</div>
                    <div className="preview-course-chip">
                      {targetCourses.find(c => c.id == selectedTargetCourse)?.courseName}
                    </div>
                  </div>
                </div>
              )}

              <div className="form-actions">
                <button
                  type="submit"
                  className="btn btn-primary"
                  disabled={loading}
                  id="submit-request-btn"
                >
                  {loading ? (
                    <>
                      <span className="spinner"></span>
                      جاري التقديم...
                    </>
                  ) : (
                    <>🔄 تقديم الطلب</>
                  )}
                </button>
                <button
                  type="button"
                  className="btn btn-outline"
                  onClick={() => onNavigate('results')}
                >
                  عرض النتائج
                </button>
              </div>
            </form>
          )}
        </div>

        {/* How It Works - Horizontal Steps */}
        <div className="how-it-works-card">
          <h3>📋 كيف تعمل المعادلة؟</h3>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(5, 1fr)', gap: '12px' }}>
            {[
              { n: 1, title: 'اختر مساقك', desc: 'من جامعتك السابقة' },
              { n: 2, title: 'المساق المستهدف', desc: 'المقابل في جامعتنا' },
              { n: 3, title: 'تحليل AI', desc: 'مقارنة المحتوى تلقائياً' },
              { n: 4, title: 'مراجعة الدكتور', desc: 'يتخذ القرار النهائي' },
              { n: 5, title: 'النتيجة', desc: 'قبول أو رفض الطلب' },
            ].map(s => (
              <div key={s.n} style={{ textAlign: 'center', padding: '12px 8px' }}>
                <div className="step-number" style={{ margin: '0 auto 10px' }}>{s.n}</div>
                <div style={{ fontSize: '12px', fontWeight: 700, color: 'var(--text-primary)', marginBottom: 4 }}>{s.title}</div>
                <div style={{ fontSize: '11px', color: 'var(--text-muted)', lineHeight: 1.4 }}>{s.desc}</div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
