// Student Courses Page
// List of courses added by the student

function StudentCoursesPage({ onNavigate }) {
  const [courses, setCourses] = React.useState([]);
  const [targetCourses, setTargetCourses] = React.useState([]);
  const [loading, setLoading] = React.useState(true);
  const [loadingTargets, setLoadingTargets] = React.useState(true);
  const [error, setError] = React.useState('');
  const [successMsg, setSuccessMsg] = React.useState('');
  const [requestError, setRequestError] = React.useState('');
  const [expandedCourseId, setExpandedCourseId] = React.useState(null);
  const [selectedTargetByCourse, setSelectedTargetByCourse] = React.useState({});
  const [submittingRequestFor, setSubmittingRequestFor] = React.useState(null);

  React.useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    setLoading(true);
    setLoadingTargets(true);
    try {
      const [coursesRes, targetRes] = await Promise.all([
        studentCoursesAPI.getAll(),
        coursesAPI.getAll().catch(() => ({ data: [] })),
      ]);

      setCourses(Array.isArray(coursesRes.data) ? coursesRes.data : []);
      setTargetCourses(Array.isArray(targetRes.data) ? targetRes.data : []);
    } catch (err) {
      console.error('Error loading courses:', err);
      setError('فشل في تحميل المساقات.');
    } finally {
      setLoading(false);
      setLoadingTargets(false);
    }
  };

  const handleDelete = async (id) => {
    if (!confirm('هل أنت متأكد أنك تريد حذف هذا المساق؟')) return;
    
    try {
      await studentCoursesAPI.delete(id);
      setCourses(courses.filter(c => c.id !== id));
    } catch (err) {
      console.error('Delete error:', err);
      alert('فشل في حذف المساق.');
    }
  };

  const toggleRequestPanel = (courseId) => {
    setRequestError('');
    setSuccessMsg('');
    setExpandedCourseId(prev => prev === courseId ? null : courseId);
  };

  const handleSubmitEquivalency = async (course) => {
    const selectedTargetId = selectedTargetByCourse[course.id];
    if (!selectedTargetId) {
      setRequestError('يرجى اختيار المساق المراد المعادلة معه أولاً.');
      return;
    }

    setSubmittingRequestFor(course.id);
    setRequestError('');
    setSuccessMsg('');
    try {
      await requestsAPI.submit({
        studentCourseId: course.id,
        targetCourseId: Number(selectedTargetId),
      });

      setSuccessMsg('تم إرسال طلب المعادلة بنجاح.');
      setExpandedCourseId(null);
      setSelectedTargetByCourse(prev => ({ ...prev, [course.id]: '' }));
    } catch (err) {
      setRequestError(err.response?.data?.message || 'فشل إرسال طلب المعادلة.');
    } finally {
      setSubmittingRequestFor(null);
    }
  };

  return (
    <div className="main-content" dir="rtl">
      <div className="page-header">
        <h1 className="page-title">مساقاتي</h1>
        <p className="page-subtitle">المساقات التي قمت بإضافتها من جامعتك السابقة.</p>
      </div>

      {error && (
        <div className="alert alert-error">
          <span>⚠️</span> {error}
        </div>
      )}

      {requestError && (
        <div className="alert alert-error">
          <span>⚠️</span> {requestError}
        </div>
      )}

      {successMsg && (
        <div className="alert alert-success">
          <span>✅</span> {successMsg}
        </div>
      )}

      <div className="table-container">
        <div className="table-header">
          <div className="table-title">قائمة المساقات ({courses.length})</div>
          <button className="btn btn-primary btn-sm" onClick={() => onNavigate('add-course')}>
            + إضافة مساق جديد
          </button>
        </div>

        {loading ? (
          <div className="loading-screen">
            <div className="spinner"></div>
            <span>جاري تحميل المساقات...</span>
          </div>
        ) : courses.length === 0 ? (
          <div className="empty-state">
            <div className="empty-state-icon">📚</div>
            <div className="empty-state-title">لا توجد مساقات بعد</div>
            <div className="empty-state-desc">
              ابدأ بإضافة المساقات التي درستها في جامعتك السابقة.
            </div>
            <button 
              className="btn btn-primary" 
              onClick={() => onNavigate('add-course')}
              style={{ marginTop: 20 }}
            >
              + أضف مساقك الأول
            </button>
          </div>
        ) : (
          <div className="table-wrapper">
            <table className="data-table">
              <thead>
                <tr>
                  <th>#</th>
                  <th>اسم المساق</th>
                  <th>الساعات المعمدة</th>
                  <th>الجامعة</th>
                  <th>الإجراءات</th>
                </tr>
              </thead>
              <tbody>
                {courses.map((course, idx) => {
                  const isExpanded = expandedCourseId === course.id;
                  const selectedTargetId = selectedTargetByCourse[course.id] || '';
                  const availableTargets = targetCourses.filter(t => String(t.id) !== String(course.id));

                  return (
                    <React.Fragment key={course.id || idx}>
                      <tr>
                        <td>{idx + 1}</td>
                        <td style={{ fontWeight: 600 }}>{course.courseName}</td>
                        <td>{course.creditHours}</td>
                        <td>{course.universityName || 'غير متوفر'}</td>
                        <td>
                          <div style={{ display: 'flex', gap: 8 }}>
                            <button
                              className="btn btn-primary btn-sm"
                              onClick={() => toggleRequestPanel(course.id)}
                            >
                              {isExpanded ? 'إغلاق' : 'طلب معادلة'}
                            </button>
                            <button
                              className="btn btn-danger btn-sm"
                              onClick={() => handleDelete(course.id)}
                              title="حذف"
                            >
                              🗑️
                            </button>
                          </div>
                        </td>
                      </tr>

                      {isExpanded && (
                        <tr>
                          <td colSpan="5" style={{ background: 'rgba(99,102,241,0.08)' }}>
                            <div style={{ padding: '14px 12px', display: 'grid', gap: 12 }}>
                              <div style={{ fontWeight: 700, fontSize: 14 }}>
                                اختر المساق المقابل من قاعدة البيانات لمعادلته مع: {course.courseName}
                              </div>

                              <div style={{ display: 'grid', gap: 10, gridTemplateColumns: 'minmax(280px, 1fr) auto' }}>
                                <select
                                  value={selectedTargetId}
                                  onChange={(e) => setSelectedTargetByCourse(prev => ({ ...prev, [course.id]: e.target.value }))}
                                  disabled={loadingTargets || submittingRequestFor === course.id}
                                >
                                  <option value="">
                                    {loadingTargets ? 'جاري تحميل مساقات الجامعة...' : 'اختر المساق المقابل'}
                                  </option>
                                  {availableTargets.map((target) => (
                                    <option key={target.id} value={target.id}>
                                      {target.courseName} ({target.creditHours ?? '-'} ساعات)
                                    </option>
                                  ))}
                                </select>

                                <button
                                  className="btn btn-success btn-sm"
                                  onClick={() => handleSubmitEquivalency(course)}
                                  disabled={submittingRequestFor === course.id || loadingTargets}
                                >
                                  {submittingRequestFor === course.id ? 'جاري الإرسال...' : 'إرسال الطلب'}
                                </button>
                              </div>
                            </div>
                          </td>
                        </tr>
                      )}
                    </React.Fragment>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
