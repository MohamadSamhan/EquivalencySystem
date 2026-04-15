// studentPreview.js - Standalone Preview Component for Students
// Modern React with "Cute & Soft" Aesthetic

function Navbar({ currentPage, onNavigate }) {
  const [mobileOpen, setMobileOpen] = React.useState(false);
  const links = [
    { id: 'student-dashboard', label: 'الرئيسية', icon: 'home' },
    { id: 'add-course', label: 'إضافة مادة', icon: 'library_books' },
    { id: 'my-courses', label: 'موادي', icon: 'list_alt' },
    { id: 'requests', label: 'طلب معادلة', icon: 'sync' },
    { id: 'results', label: 'النتائج', icon: 'analytics' },
  ];

  return (
    <>
      <div className="mobile-header">
        <button className="mobile-menu-btn" onClick={() => setMobileOpen(!mobileOpen)}>
          <span className="material-icons">menu</span>
        </button>
        <span className="mobile-brand-title">نظام معادلة المواد</span>
      </div>
      
      {mobileOpen && <div className="sidebar-overlay" onClick={() => setMobileOpen(false)} />}
      
      <nav className={`sidebar ${mobileOpen ? 'open' : ''}`} dir="rtl">
        <div className="sidebar-header">
          <div className="sidebar-brand">
            <div className="sidebar-brand-icon">
              <span className="material-icons">school</span>
            </div>
            <div>
              <div className="sidebar-brand-text">نظام المعادلة</div>
              <div className="sidebar-brand-sub">بوابة الطالب</div>
            </div>
          </div>
        </div>
        
        <div className="sidebar-nav">
          <div className="sidebar-section-label">القائمة الرئيسية</div>
          {links.map((link) => (
            <button 
              key={link.id} 
              className={`sidebar-link ${currentPage === link.id ? 'active' : ''}`} 
              onClick={() => { onNavigate(link.id); setMobileOpen(false); }}
            >
              <span className="material-icons sidebar-link-icon">{link.icon}</span>
              <span className="sidebar-link-text">{link.label}</span>
            </button>
          ))}
        </div>
        
        <div className="sidebar-footer">
          <div className="sidebar-user">
            <div className="sidebar-user-avatar">أ</div>
            <div className="sidebar-user-info">
              <div className="sidebar-user-name">أحمد المعاين</div>
              <div className="sidebar-user-role">وضع التجربة</div>
            </div>
          </div>
          <button className="logout-btn" onClick={() => window.location.href = 'index.html'}>
            <span className="material-icons sidebar-link-icon">logout</span>
            تسجيل الخروج
          </button>
        </div>
      </nav>
    </>
  );
}

function StudentDashboard({ onNavigate }) {
  const stats = [
    { label: 'المواد المضافة', value: 5, icon: 'library_books', color: 'blue' },
    { label: 'قيد الانتظار', value: 2, icon: 'pending_actions', color: 'orange' },
    { label: 'معتمدة', value: 3, icon: 'check_circle', color: 'green' },
    { label: 'مرفوضة', value: 1, icon: 'cancel', color: 'red' },
  ];

  const quickActions = [
    { id: 'add-course', title: 'إضافة مادة', icon: 'add_circle', desc: 'إضافة محتوى أكاديمي جديد.' },
    { id: 'requests', title: 'طلب معادلة', icon: 'compare_arrows', desc: 'بدء عملية معادلة موادك.' },
  ];

  return (
    <div className="page-container glass-effect">
      <div className="page-header">
        <h1 className="page-title animate-fade-in">مرحباً بك في وضع المعاينة</h1>
        <p className="page-subtitle">هذه البيانات تجريبية مخصصة لعرض التصميم الجديد.</p>
      </div>

      <div className="stats-grid">
        {stats.map((s, i) => (
          <div key={i} className="stat-card glass-card">
            <div className={`stat-icon-wrapper ${s.color}`}>
              <span className="material-icons">{s.icon}</span>
            </div>
            <div className="stat-content">
              <div className="stat-value">{s.value}</div>
              <div className="stat-label">{s.label}</div>
            </div>
          </div>
        ))}
      </div>

      <div className="section-title">إجراءات سريعة</div>
      <div className="actions-grid">
        {quickActions.map(action => (
          <div key={action.id} className="action-card-modern glass-card" onClick={() => onNavigate(action.id)}>
            <span className="material-icons action-icon">{action.icon}</span>
            <div className="action-info">
              <div className="action-title">{action.title}</div>
              <div className="action-desc">{action.desc}</div>
            </div>
            <span className="material-icons arrow">chevron_left</span>
          </div>
        ))}
      </div>
    </div>
  );
}

// Fallback for missing pages in preview
const PlaceholderPage = ({ title }) => (
  <div className="page-container glass-effect">
    <div className="page-header">
      <h1 className="page-title">{title}</h1>
      <p className="page-subtitle">هذه الصفحة قيد التطوير في وضع المعاينة.</p>
    </div>
    <div className="empty-state-visual glass-card">
      <span className="material-icons large-icon">construction</span>
      <p>قريباً سيتم تفعيل كامل الوظائف هنا.</p>
    </div>
  </div>
);

function App() {
  const [page, setPage] = React.useState('student-dashboard');

  const renderContent = () => {
    switch (page) {
      case 'student-dashboard': return <StudentDashboard onNavigate={setPage} />;
      case 'add-course': return <PlaceholderPage title="إضافة مادة" />;
      case 'my-courses': return <PlaceholderPage title="موادي" />;
      case 'requests': return <PlaceholderPage title="طلب معادلة" />;
      case 'results': return <PlaceholderPage title="النتائج" />;
      default: return <StudentDashboard onNavigate={setPage} />;
    }
  };

  return (
    <div className="app-layout" dir="rtl">
      <Navbar currentPage={page} onNavigate={setPage} />
      <main className="main-content-wrapper">
        {renderContent()}
      </main>
    </div>
  );
}

const root = ReactDOM.createRoot(document.getElementById('root'));
root.render(<App />);
