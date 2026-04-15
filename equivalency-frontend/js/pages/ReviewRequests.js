// Review Requests Page (Doctor)
// Review and decide on student course equivalency requests

function ReviewRequestsPage({ onNavigate }) {
  const [requests, setRequests] = React.useState([]);
  const [loading, setLoading] = React.useState(true);
  const [actionLoading, setActionLoading] = React.useState(null);
  const [filter, setFilter] = React.useState('قيد الانتظار');
  const [successMsg, setSuccessMsg] = React.useState('');

  const [studentCourses, setStudentCourses] = React.useState([]);
  const [targetCourses, setTargetCourses] = React.useState([]);

  const handleDownloadFile = async (fileUrl) => {
    if (!fileUrl) return;
    try {
      const token = localStorage.getItem('token');
      const fullUrl = fileUrl.startsWith('http') ? fileUrl : `https://localhost:7012${fileUrl}`;
      const res = await fetch(fullUrl, {
        headers: { Authorization: `Bearer ${token}` }
      });
      if (!res.ok) { alert('تعذّر تحميل الملف.'); return; }
      const blob = await res.blob();
      const contentDisposition = res.headers.get('content-disposition') || '';
      const nameMatch = contentDisposition.match(/filename\*?=(?:UTF-8'')?([^;\n"]+)/i);
      const fileName = nameMatch ? decodeURIComponent(nameMatch[1].replace(/"/g, '')) : 'student_file';
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = fileName;
      a.target = '_blank';
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    } catch (e) {
      alert('حدث خطأ أثناء تحميل الملف.');
    }
  };
  React.useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      const [reqRes, stuRes, tarRes] = await Promise.all([
        requestsAPI.getAll().catch(() => ({ data: [] })),
        studentCoursesAPI.getAll().catch(() => ({ data: [] })),
        coursesAPI.getAll().catch(() => ({ data: [] }))
      ]);
      const reqs = Array.isArray(reqRes.data) ? reqRes.data : [];
      console.log('📋 Requests from API:', JSON.stringify(reqs.slice(0,2), null, 2));
      setRequests(reqs);
      setStudentCourses(Array.isArray(stuRes.data) ? stuRes.data : []);
      setTargetCourses(Array.isArray(tarRes.data) ? tarRes.data : []);
    } catch (err) {
      console.error('Error loading requests:', err);
    } finally {
      setLoading(false);
    }
  };

  const getStatusString = (statusCode) => {
    if (statusCode === 0 || statusCode === 'Pending') return 'Pending';
    if (statusCode === 1 || statusCode === 'Approved') return 'Approved';
    if (statusCode === 2 || statusCode === 'Rejected') return 'Rejected';
    return String(statusCode); 
  };

  const handleApprove = async (id) => {
    setActionLoading(id);
    try {
      await requestsAPI.approve(id);
      setRequests(requests.map(r => r.id === id ? { ...r, status: 'Approved' } : r));
      setSuccessMsg('تمت الموافقة على الطلب! ✅');
      setTimeout(() => setSuccessMsg(''), 3000);
    } catch (err) {
      console.error('Approve error:', err);
      alert('فشل في الموافقة على الطلب.');
    } finally {
      setActionLoading(null);
    }
  };

  const handleReject = async (id) => {
    if (!confirm('هل أنت متأكد أنك تريد رفض هذا الطلب؟')) return;
    
    setActionLoading(id);
    try {
      await requestsAPI.reject(id);
      setRequests(requests.map(r => r.id === id ? { ...r, status: 'Rejected' } : r));
      setSuccessMsg('تم رفض الطلب. ❌');
      setTimeout(() => setSuccessMsg(''), 3000);
    } catch (err) {
      console.error('Reject error:', err);
      alert('فشل في رفض الطلب.');
    } finally {
      setActionLoading(null);
    }
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
        <h1 className="page-title">مراجعة الطلبات</h1>
        <p className="page-subtitle">مراجعة واتخاذ قرار بشأن طلبات معادلة المساقات للطلاب.</p>
      </div>

      {successMsg && (
        <div className="alert alert-success">
          <span>✅</span> {successMsg}
        </div>
      )}

      {/* Stats Summary */}
      <div className="dashboard-grid" style={{ marginBottom: 24 }}>
        <div className="stat-card">
          <div className="stat-card-icon orange">⏳</div>
          <div className="stat-card-value">
            {requests.filter(r => getStatusString(r.status) === 'Pending').length}
          </div>
          <div className="stat-card-label">قيد الانتظار</div>
        </div>
        <div className="stat-card">
          <div className="stat-card-icon green">✅</div>
          <div className="stat-card-value">
            {requests.filter(r => getStatusString(r.status) === 'Approved').length}
          </div>
          <div className="stat-card-label">تمت الموافقة</div>
        </div>
        <div className="stat-card">
          <div className="stat-card-icon red">❌</div>
          <div className="stat-card-value">
            {requests.filter(r => getStatusString(r.status) === 'Rejected').length}
          </div>
          <div className="stat-card-label">تم الرفض</div>
        </div>
      </div>

      {/* Tabs */}
      <div className="filter-tabs">
        {['قيد الانتظار', 'تمت الموافقة', 'تم الرفض', 'الكل'].map((tab) => (
          <button
            key={tab}
            className={`btn btn-sm ${filter === tab ? 'btn-primary' : 'btn-outline'}`}
            onClick={() => setFilter(tab)}
            style={{ animationName: 'none' }}
          >
            {tab}
            <span style={{ marginRight: 6, opacity: 0.7 }}>
              ({tab === 'الكل' ? requests.length : requests.filter(r => {
                const s = getStatusString(r.status);
                if (tab === 'قيد الانتظار') return s === 'Pending';
                if (tab === 'تمت الموافقة') return s === 'Approved';
                if (tab === 'تم الرفض') return s === 'Rejected';
                return false;
              }).length})
            </span>
          </button>
        ))}
      </div>

      <div className="table-container">
        <div className="table-header">
          <div className="table-title">طلبات {filter} ({filteredRequests.length})</div>
        </div>

        {loading ? (
          <div className="loading-screen">
            <div className="spinner"></div>
            <span>جاري تحميل الطلبات...</span>
          </div>
        ) : filteredRequests.length === 0 ? (
          <div className="empty-state">
            <div className="empty-state-icon">📝</div>
            <div className="empty-state-title">لا توجد طلبات {filter}</div>
            <div className="empty-state-desc">لا توجد طلبات مراجعة في الوقت الحالي.</div>
          </div>
        ) : (
          <div className="table-wrapper">
            <table className="data-table">
              <thead>
                <tr>
                  <th>#</th>
                  <th>اسم الطالب</th>
                  <th>مساق الطالب</th>
                  <th>المساق المستهدف</th>
                  <th>نسبة التشابه</th>
                  <th>الملف</th>
                  <th>الحالة</th>
                  <th>الإجراء</th>
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

                  const sCourseName = getCourseName(sCourse, req.studentCourse, req.studentCourseName, `مساق طالب #${req.studentCourseId || ''}`);
                  const tCourseName = getCourseName(tCourse, req.targetCourse, req.targetCourseName, `مساق جامعي #${req.targetCourseId || ''}`);
                  const sim = req.similarity ?? req.similarityScore ?? req.similarityPercentage ?? null;
                  const sName = req.studentName || req.student?.name || req.student?.userName || `طالب #${req.studentId || ''}`;
                  const fileUrl = req.studentFileUrl || null;

                  return (
                  <tr key={req.id || idx}>
                    <td>{idx + 1}</td>
                    <td style={{ fontWeight: 600 }}>{sName}</td>
                    <td>{sCourseName}</td>
                    <td>{tCourseName}</td>
                    <td>
                      {sim != null ? (
                        <div className="similarity-bar">
                          <div className="similarity-track">
                             <div 
                              className={`similarity-fill ${sim >= 70 ? 'high' : sim >= 40 ? 'medium' : 'low'}`} 
                              style={{ width: `${sim}%` }}
                            />
                          </div>
                          <span style={{ 
                            fontSize: 12, 
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
                      {fileUrl ? (
                        <button
                          className="btn btn-outline btn-sm"
                          onClick={() => handleDownloadFile(fileUrl)}
                          title="عرض ملف الطالب"
                          style={{ gap: 4 }}
                        >
                          📄 عرض الملف
                        </button>
                      ) : (
                        <span style={{ color: '#64748b', fontSize: 12 }}>لا يوجد</span>
                      )}
                    </td>
                    <td>
                      <span className={`badge ${getBadgeClass(req.status)}`}>
                        <span className="badge-dot"></span>
                        {getStatusText(req.status)}
                      </span>
                    </td>
                    <td>
                      {getStatusString(req.status) === 'Pending' ? (
                        <div style={{ display: 'flex', gap: 8 }}>
                          <button 
                            className="btn btn-success btn-sm"
                            onClick={() => handleApprove(req.id)}
                            disabled={actionLoading === req.id}
                          >
                            {actionLoading === req.id ? '...' : '✅ موافقة'}
                          </button>
                          <button 
                            className="btn btn-danger btn-sm"
                            onClick={() => handleReject(req.id)}
                            disabled={actionLoading === req.id}
                          >
                            {actionLoading === req.id ? '...' : '❌ رفض'}
                          </button>
                        </div>
                      ) : (
                        <span style={{ color: '#64748b', fontSize: 13 }}>{getStatusText(req.status)}</span>
                      )}
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
