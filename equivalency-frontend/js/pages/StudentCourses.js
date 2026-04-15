// Student Courses Page
// List of courses added by the student

function StudentCoursesPage({ onNavigate }) {
  const [courses, setCourses] = React.useState([]);
  const [loading, setLoading] = React.useState(true);
  const [error, setError] = React.useState('');

  React.useEffect(() => {
    loadCourses();
  }, []);

  const loadCourses = async () => {
    try {
      const res = await studentCoursesAPI.getAll();
      setCourses(Array.isArray(res.data) ? res.data : []);
    } catch (err) {
      console.error('Error loading courses:', err);
      setError('فشل في تحميل المساقات.');
    } finally {
      setLoading(false);
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
                {courses.map((course, idx) => (
                  <tr key={course.id || idx}>
                    <td>{idx + 1}</td>
                    <td style={{ fontWeight: 600 }}>{course.courseName}</td>
                    <td>{course.creditHours}</td>
                    <td>{course.universityName || 'غير متوفر'}</td>
                    <td>
                      <div style={{ display: 'flex', gap: 8 }}>
                        <button 
                          className="btn btn-primary btn-sm"
                          onClick={() => onNavigate('requests')}
                        >
                          طلب معادلة
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
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
