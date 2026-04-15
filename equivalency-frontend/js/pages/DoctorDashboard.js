// Doctor Dashboard Page
// Overview of requests awaiting review

function DoctorDashboard({ onNavigate }) {
  const { user } = useAuth();
  const userName = user?.name || localStorage.getItem('userName') || localStorage.getItem('fullName') || '';
  const [stats, setStats] = React.useState({ pending: 0, approved: 0, rejected: 0, total: 0 });
  const [recentRequests, setRecentRequests] = React.useState([]);
  const [loading, setLoading] = React.useState(true);

  React.useEffect(() => {
    loadRequests();
  }, []);

  const loadRequests = async () => {
    try {
      const res = await requestsAPI.getAll().catch(() => ({ data: [] }));
      const requests = Array.isArray(res.data) ? res.data : [];
      
      const getStatusType = (s) => {
        if (s === 0 || s === 'Pending') return 'Pending';
        if (s === 1 || s === 'Approved') return 'Approved';
        if (s === 2 || s === 'Rejected') return 'Rejected';
        return String(s);
      };

      setStats({
        total: requests.length,
        pending: requests.filter(r => getStatusType(r.status) === 'Pending').length,
        approved: requests.filter(r => getStatusType(r.status) === 'Approved').length,
        rejected: requests.filter(r => getStatusType(r.status) === 'Rejected').length,
      });
      setRecentRequests(requests.filter(r => getStatusType(r.status) === 'Pending').slice(0, 5));
    } catch (err) {
      console.error('Error loading requests:', err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="main-content" dir="rtl">
      <div className="page-header">
        <h1 id="welcome" className="page-title">مرحباً يا دكتور {userName ? userName.split(' ')[0] : ''} 👋</h1>
        <p className="page-subtitle">نظرة عامة على طلبات معادلة المساقات التي تنتظر مراجعتك.</p>
      </div>

      {/* Stats Grid */}
      <div className="dashboard-grid">
        <div className="stat-card">
          <div className="stat-card-icon blue">📝</div>
          <div className="stat-card-value">{loading ? '...' : stats.total}</div>
          <div className="stat-card-label">إجمالي الطلبات</div>
        </div>
        <div className="stat-card">
          <div className="stat-card-icon orange">⏳</div>
          <div className="stat-card-value">{loading ? '...' : stats.pending}</div>
          <div className="stat-card-label">قيد المراجعة</div>
        </div>
        <div className="stat-card">
          <div className="stat-card-icon green">✅</div>
          <div className="stat-card-value">{loading ? '...' : stats.approved}</div>
          <div className="stat-card-label">تمت الموافقة</div>
        </div>
        <div className="stat-card">
          <div className="stat-card-icon red">❌</div>
          <div className="stat-card-value">{loading ? '...' : stats.rejected}</div>
          <div className="stat-card-label">تم الرفض</div>
        </div>
      </div>

      <div className="action-cards">
        <div 
          className="action-card" 
          onClick={() => onNavigate('review-requests')} 
          tabIndex={0} 
          role="button"
          onKeyDown={(e) => e.key === 'Enter' && onNavigate('review-requests')}
        >
          <div className="action-card-icon stat-card-icon blue">📝</div>
          <div className="action-card-title">مراجعة الطلبات المعلقة</div>
          <div className="action-card-desc">مراجعة طلبات معادلة الطلاب والموافقة عليها أو رفضها.</div>
          <div className="action-card-arrow">انتقل إلى المراجعات ←</div>
        </div>
      </div>

      {recentRequests.length > 0 && (
        <div className="table-container" style={{ marginTop: 28 }}>
          <div className="table-header">
            <div className="table-title">آخر الطلبات المعلقة</div>
            <button className="btn btn-outline btn-sm" onClick={() => onNavigate('review-requests')}>
              عرض الكل ←
            </button>
          </div>
          <div className="table-wrapper">
            <table className="data-table">
              <thead>
                <tr>
                  <th>الطالب</th>
                  <th>مساق الطالب</th>
                  <th>المساق المستهدف</th>
                  <th>الحالة</th>
                </tr>
              </thead>
              <tbody>
                {recentRequests.map((req, idx) => (
                  <tr key={idx}>
                    <td style={{ fontWeight: 600, color: '#e2e8f0' }}>{req.studentName || 'غير معروف'}</td>
                    <td>{req.studentCourseName || 'غير معروف'}</td>
                    <td>{req.targetCourseName || 'غير معروف'}</td>
                    <td>
                      <span className="badge badge-pending">
                        <span className="badge-dot"></span>
                        قيد الانتظار
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
