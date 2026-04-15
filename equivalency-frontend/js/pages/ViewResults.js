// View Results Page (Student)
// Track status of submitted equivalency requests

function ViewResultsPage({ onNavigate }) {
  const [requests, setRequests] = React.useState([]);
  const [studentCourses, setStudentCourses] = React.useState([]);
  const [targetCourses, setTargetCourses] = React.useState([]);
  const [loading, setLoading] = React.useState(true);
  const [filter, setFilter] = React.useState('الكل');

  React.useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      const [reqRes, stuRes, tarRes] = await Promise.all([
        requestsAPI.getMyRequests().catch(() => ({ data: [] })),
        studentCoursesAPI.getAll().catch(() => ({ data: [] })),
        coursesAPI.getAll().catch(() => ({ data: [] }))
      ]);
      setRequests(Array.isArray(reqRes.data) ? reqRes.data : []);
      setStudentCourses(Array.isArray(stuRes.data) ? stuRes.data : []);
      setTargetCourses(Array.isArray(tarRes.data) ? tarRes.data : []);
    } catch (err) {
      console.error('Error loading results:', err);
    } finally {
      setLoading(false);
    }
  };

  const getStatusString = (statusCode) => {
    // Handling numeric statuses since the API might return 0, 1, 2
    if (statusCode === 0 || statusCode === 'Pending') return 'Pending';
    if (statusCode === 1 || statusCode === 'Approved') return 'Approved';
    if (statusCode === 2 || statusCode === 'Rejected') return 'Rejected';
    return String(statusCode); 
  };

  const filteredRequests = filter === 'الكل' 
    ? requests 
    : requests.filter(r => {
        const s = getStatusString(r.status);
        if (filter === 'قيد الانتظار') return s === 'Pending';
        if (filter === 'تمت الموافقة') return s === 'Approved';
        if (filter === 'تم الرفض') return s === 'Rejected';
        return true;
      });

  const getSimilarityColor = (similarity) => {
    if (similarity >= 70) return 'high';
    if (similarity >= 40) return 'medium';
    return 'low';
  };

  const getBadgeClass = (status) => {
    const s = getStatusString(status);
    switch (s) {
      case 'Approved': return 'badge-approved';
      case 'Rejected': return 'badge-rejected';
      default: return 'badge-pending';
    }
  };

  const getStatusText = (status) => {
    const s = getStatusString(status);
    switch (s) {
      case 'Approved': return 'تمت الموافقة';
      case 'Rejected': return 'تم الرفض';
      default: return 'قيد الانتظار';
    }
  };

  return (
    <div className="main-content" dir="rtl">
      <div className="page-header">
        <h1 className="page-title">نتائج المعادلة</h1>
        <p className="page-subtitle">تتبع حالة ونتائج طلبات المعادلة الخاصة بك.</p>
      </div>

      {/* Filter Tabs */}
      <div className="filter-tabs">
        {['الكل', 'قيد الانتظار', 'تمت الموافقة', 'تم الرفض'].map((tab) => (
          <button
            key={tab}
            className={`btn btn-sm ${filter === tab ? 'btn-primary' : 'btn-outline'}`}
            onClick={() => setFilter(tab)}
            style={{ animationName: 'none' }}
          >
            {tab}
            {tab !== 'الكل' && (
               <span style={{ marginRight: 6, opacity: 0.7 }}>
                ({requests.filter(r => {
                  const s = getStatusString(r.status);
                  if (tab === 'قيد الانتظار') return s === 'Pending';
                  if (tab === 'تمت الموافقة') return s === 'Approved';
                  if (tab === 'تم الرفض') return s === 'Rejected';
                  return false;
                }).length})
               </span>
            )}
          </button>
        ))}
      </div>

      <div className="table-container">
        <div className="table-header">
          <div className="table-title">النتائج ({filteredRequests.length})</div>
          <button className="btn btn-primary btn-sm" onClick={() => onNavigate('requests')}>
            + طلب جديد
          </button>
        </div>

        {loading ? (
          <div className="loading-screen">
            <div className="spinner"></div>
            <span>جاري تحميل النتائج...</span>
          </div>
        ) : filteredRequests.length === 0 ? (
          <div className="empty-state">
            <div className="empty-state-icon">📊</div>
            <div className="empty-state-title">
              {filter === 'الكل' ? 'لا توجد طلبات بعد' : `لا توجد طلبات ${filter}`}
            </div>
            <div className="empty-state-desc">
              {filter === 'الكل' 
                ? 'ابدأ بتقديم أول طلب معادلة لك.' 
                : `لم يتم العثور على طلبات بحالة ${filter}.`}
            </div>
          </div>
        ) : (
          <div className="table-wrapper">
            <table className="data-table">
              <thead>
                <tr>
                  <th>#</th>
                  <th>مساقي</th>
                  <th>المساق المستهدف</th>
                  <th>نسبة التشابه</th>
                  <th>الحالة</th>
                </tr>
              </thead>
              <tbody>
                {filteredRequests.map((req, idx) => {
                  // Try ID-based lookup first, then handle nested objects from API
                  const sCourse = studentCourses.find(c => c.id === req.studentCourseId || c.id === req.studentCourse?.id);
                  const tCourse = targetCourses.find(c => c.id === req.targetCourseId || c.id === req.targetCourse?.id);

                  // Extract name: prioritize lookup table, then nested object, then flat field
                  const getCourseName = (lookedUp, nested, flatName, fallback) => {
                    if (lookedUp && typeof lookedUp.courseName === 'string') return lookedUp.courseName;
                    if (nested && typeof nested === 'object' && nested.courseName) return nested.courseName;
                    if (typeof flatName === 'string' && flatName) return flatName;
                    return fallback;
                  };

                  const sCourseName = getCourseName(sCourse, req.studentCourse, req.studentCourseName, 'مساق غير معروف');
                  const tCourseName = getCourseName(tCourse, req.targetCourse, req.targetCourseName, 'مساق غير معروف');
                  const sim = req.similarity ?? req.similarityScore ?? req.similarityPercentage ?? null;

                  return (
                  <tr key={req.id || idx}>
                    <td>{idx + 1}</td>
                    <td style={{ fontWeight: 600 }}>{sCourseName}</td>
                    <td>{tCourseName}</td>
                    <td>
                      {sim != null ? (
                        <div className="similarity-bar">
                          <div className="similarity-track">
                            <div 
                              className={`similarity-fill ${getSimilarityColor(sim)}`} 
                              style={{ width: `${sim}%` }}
                            />
                          </div>
                          <span className="similarity-value" style={{ 
                            color: sim >= 70 ? '#22c55e' : sim >= 40 ? '#f59e0b' : '#ef4444' 
                          }}>
                            {sim}%
                          </span>
                        </div>
                      ) : (
                        <span style={{ color: '#64748b' }}>—</span>
                      )}
                    </td>
                    <td>
                      <span className={`badge ${getBadgeClass(req.status)}`}>
                        <span className="badge-dot"></span>
                        {getStatusText(req.status)}
                      </span>
                    </td>
                  </tr>
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
