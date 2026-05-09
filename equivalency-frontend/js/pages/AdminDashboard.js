// Admin Dashboard Page
// Overview cards with quick stats and navigation to management pages

function AdminDashboard({ onNavigate }) {
  const { user } = useAuth();
  const userName = user?.name || localStorage.getItem('userName') || localStorage.getItem('fullName') || '';
  const [stats, setStats] = React.useState({ universities: 0, courses: 0, departments: 0, users: 0 });
  const [loading, setLoading] = React.useState(true);

  React.useEffect(() => {
    loadStats();
  }, []);

  const loadStats = async () => {
    try {
      const [uniRes, courseRes, deptRes, userRes] = await Promise.all([
        adminUniversitiesAPI.getAll().catch(() => ({ data: [] })),
        adminCoursesAPI.getAll().catch(() => ({ data: [] })),
        adminDepartmentsAPI.getAll().catch(() => ({ data: [] })),
        adminUsersAPI.getAll().catch(() => ({ data: [] })),
      ]);
      setStats({
        universities: (Array.isArray(uniRes.data) ? uniRes.data : []).length,
        courses: (Array.isArray(courseRes.data) ? courseRes.data : []).length,
        departments: (Array.isArray(deptRes.data) ? deptRes.data : []).length,
        users: (Array.isArray(userRes.data) ? userRes.data : []).length,
      });
    } catch (err) {
      console.error('Error loading admin stats:', err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="main-content" dir="rtl">
      <div className="page-header">
        <h1 className="page-title">مرحباً يا مدير {userName ? userName.split(' ')[0] : ''} 👋</h1>
        <p className="page-subtitle">لوحة التحكم الإدارية — إدارة بيانات النظام بسهولة.</p>
      </div>

      {/* Stats Grid */}
      <div className="dashboard-grid">
        <div className="stat-card">
          <div className="stat-card-icon blue">🏛️</div>
          <div className="stat-card-value">{loading ? '...' : stats.universities}</div>
          <div className="stat-card-label">الجامعات</div>
        </div>
        <div className="stat-card">
          <div className="stat-card-icon green">📚</div>
          <div className="stat-card-value">{loading ? '...' : stats.courses}</div>
          <div className="stat-card-label">المساقات</div>
        </div>
        <div className="stat-card">
          <div className="stat-card-icon orange">🏢</div>
          <div className="stat-card-value">{loading ? '...' : stats.departments}</div>
          <div className="stat-card-label">الأقسام</div>
        </div>
        <div className="stat-card">
          <div className="stat-card-icon purple">👥</div>
          <div className="stat-card-value">{loading ? '...' : stats.users}</div>
          <div className="stat-card-label">المستخدمين</div>
        </div>
      </div>

      {/* Quick Actions */}
      <h2 className="section-title" style={{ marginTop: 32 }}>إجراءات سريعة</h2>
      <div className="action-cards">
        <div className="action-card" onClick={() => onNavigate('admin-training-requests')}>
          <span className="action-card-icon">🎓</span>
          <h3 className="action-card-title">طلبات الشهادات التدريبية</h3>
          <p className="action-card-desc">عرض مخصص لطلبات معادلة الشهادات التدريبية بدون صلاحية قرار.</p>
        </div>
        <div className="action-card" onClick={() => onNavigate('manage-universities')}>
          <span className="action-card-icon">🏛️</span>
          <h3 className="action-card-title">إدارة الجامعات</h3>
          <p className="action-card-desc">إضافة وتعديل الجامعات المسجلة في النظام</p>
        </div>
        <div className="action-card" onClick={() => onNavigate('manage-courses')}>
          <span className="action-card-icon">📚</span>
          <h3 className="action-card-title">إدارة المساقات</h3>
          <p className="action-card-desc">إضافة وتعديل المساقات الجامعية</p>
        </div>
        <div className="action-card" onClick={() => onNavigate('manage-departments')}>
          <span className="action-card-icon">🏢</span>
          <h3 className="action-card-title">إدارة الأقسام</h3>
          <p className="action-card-desc">إضافة وتعديل الأقسام الأكاديمية</p>
        </div>
        <div className="action-card" onClick={() => onNavigate('manage-users')}>
          <span className="action-card-icon">👥</span>
          <h3 className="action-card-title">إدارة المستخدمين</h3>
          <p className="action-card-desc">إنشاء وتعديل وحذف حسابات الطلاب والدكاترة</p>
        </div>
      </div>
    </div>
  );
}
