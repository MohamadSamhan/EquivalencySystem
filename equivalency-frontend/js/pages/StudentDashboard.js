// Student Dashboard Page
// Overview with stats and quick-action cards

function StudentDashboard({ onNavigate }) {
  const { user } = useAuth();
  const userName = user?.name || localStorage.getItem('userName') || localStorage.getItem('fullName') || '';
  const [stats, setStats] = React.useState({ courses: 0, pending: 0, approved: 0, rejected: 0 });
  const [loading, setLoading] = React.useState(true);

  React.useEffect(() => {
    loadStats();
  }, []);

  const loadStats = async () => {
    try {
      const [coursesRes, requestsRes] = await Promise.all([
        studentCoursesAPI.getAll().catch(() => ({ data: [] })),
        requestsAPI.getMyRequests().catch(() => ({ data: [] })),
      ]);
      
      const courses = Array.isArray(coursesRes.data) ? coursesRes.data : [];
      const requests = Array.isArray(requestsRes.data) ? requestsRes.data : [];

      const getStatusType = (s) => {
        if (s === 0 || s === 'Pending') return 'Pending';
        if (s === 1 || s === 'Approved') return 'Approved';
        if (s === 2 || s === 'Rejected') return 'Rejected';
        return String(s);
      };

      setStats({
        courses: courses.length,
        pending: requests.filter(r => getStatusType(r.status) === 'Pending').length,
        approved: requests.filter(r => getStatusType(r.status) === 'Approved').length,
        rejected: requests.filter(r => getStatusType(r.status) === 'Rejected').length,
      });
    } catch (err) {
      console.error('Error loading stats:', err);
    } finally {
      setLoading(false);
    }
  };

  const actionCards = [
    {
      id: 'add-course',
      icon: '📚',
      iconBg: 'blue',
      title: 'إضافة مساق',
      desc: 'إضافة المساقات التي أكملتها في جامعتك السابقة.',
    },
    {
      id: 'my-courses',
      icon: '📋',
      iconBg: 'teal',
      title: 'مساقاتي',
      desc: 'عرض وإدارة جميع المساقات التي قمت بإضافتها.',
    },
    {
      id: 'requests',
      icon: '🔄',
      iconBg: 'green',
      title: 'طلب معادلة',
      desc: 'تقديم طوي جديد لمعادلة المساقات.',
    },
    {
      id: 'results',
      icon: '📊',
      iconBg: 'orange',
      title: 'عرض النتائج',
      desc: 'التحقق من حالة ونتائج طلباتك.',
    },
  ];

  return (
    <div className="main-content" dir="rtl">
      <div className="page-header">
        <h1 id="welcome" className="page-title">مرحباً يا {userName ? userName.split(' ')[0] : ''} 👋</h1>
        <p className="page-subtitle">إليك نظرة عامة على نشاط معادلة المساقات الخاص بك.</p>
      </div>

      {/* Stats Grid */}
      <div className="dashboard-grid">
        <div className="stat-card">
          <div className="stat-card-icon blue">📚</div>
          <div className="stat-card-value">{loading ? '...' : stats.courses}</div>
          <div className="stat-card-label">المساقات المضافة</div>
        </div>
        <div className="stat-card">
          <div className="stat-card-icon orange">⏳</div>
          <div className="stat-card-value">{loading ? '...' : stats.pending}</div>
          <div className="stat-card-label">طلبات قيد الانتظار</div>
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

      {/* Action Cards */}
      <div className="section-title" style={{ marginTop: 8 }}>إجراءات سريعة</div>
      <div className="action-cards">
        {actionCards.map((card) => (
          <div
            key={card.id}
            className="action-card"
            onClick={() => onNavigate(card.id)}
            tabIndex={0}
            role="button"
            onKeyDown={(e) => e.key === 'Enter' && onNavigate(card.id)}
          >
            <div className={`action-card-icon stat-card-icon ${card.iconBg}`}>
              {card.icon}
            </div>
            <div className="action-card-title">{card.title}</div>
            <div className="action-card-desc">{card.desc}</div>
            <div className="action-card-arrow">
              انتقل إلى {card.title} ←
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
