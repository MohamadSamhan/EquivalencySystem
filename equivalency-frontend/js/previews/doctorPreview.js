// doctorPreview.js - Standalone Preview Component for Doctors
// Professional Academic Aesthetic with "Cute & Soft" Refinements

function Navbar({ currentPage, onNavigate }) {
  const [mobileOpen, setMobileOpen] = React.useState(false);
  const links = [
    { id: 'doctor-dashboard', label: 'الرئيسية', icon: 'dashboard' },
    { id: 'review-requests', label: 'مراجعة الطلبات', icon: 'rate_review' },
  ];

  return (
    <>
      <div className="mobile-header doctor-theme">
        <button className="mobile-menu-btn" onClick={() => setMobileOpen(!mobileOpen)}>
          <span className="material-icons">menu</span>
        </button>
        <span className="mobile-brand-title">بوابة مراجعة المواد</span>
      </div>
      
      {mobileOpen && <div className="sidebar-overlay" onClick={() => setMobileOpen(false)} />}
      
      <nav className={`sidebar ${mobileOpen ? 'open' : ''}`} dir="rtl">
        <div className="sidebar-header">
          <div className="sidebar-brand">
            <div className="sidebar-brand-icon doctor-icon">
              <span className="material-icons">architecture</span>
            </div>
            <div>
              <div className="sidebar-brand-text">نظام المعادلة</div>
              <div className="sidebar-brand-sub">بوابة الدكتور</div>
            </div>
          </div>
        </div>
        
        <div className="sidebar-nav">
          <div className="sidebar-section-label">قائمة التحكم</div>
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
            <div className="sidebar-user-avatar doctor-avatar">د</div>
            <div className="sidebar-user-info">
              <div className="sidebar-user-name">د. محمد الخبير</div>
              <div className="sidebar-user-role">رئيس لجنة المعادلات</div>
            </div>
          </div>
          <button className="logout-btn" onClick={() => window.location.href = 'index.html'}>
            <span className="material-icons sidebar-link-icon">logout</span>
            خروج من المعاينة
          </button>
        </div>
      </nav>
    </>
  );
}

function DoctorDashboard({ onNavigate }) {
  const stats = [
    { label: 'إجمالي الطلبات', value: 145, icon: 'assignment', color: 'blue' },
    { label: 'بانتظار المراجعة', value: 12, icon: 'hourglass_empty', color: 'orange' },
    { label: 'تمت الموافقة', value: 128, icon: 'verified', color: 'green' },
    { label: 'تم الرفض', value: 5, icon: 'report_problem', color: 'red' },
  ];

  return (
    <div className="page-container glass-effect">
      <div className="page-header">
        <h1 className="page-title">مرحباً دكتور</h1>
        <p className="page-subtitle">نظرة عامة وشاملة على طلبات المعادلة المعلقة.</p>
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

      <div className="section-title">إجراءات المراجعة</div>
      <div className="actions-grid">
        <div className="action-card-modern glass-card highlight-blue" onClick={() => onNavigate('review-requests')}>
          <span className="material-icons action-icon">checklist</span>
          <div className="action-info">
            <div className="action-title">بدء مراجعة الطلبات</div>
            <div className="action-desc">تقييم التشابه الأكاديمي والبت في القرارات.</div>
          </div>
          <span className="material-icons arrow">chevron_left</span>
        </div>
      </div>
    </div>
  );
}

// Fallback Page
const PlaceholderPage = ({ title }) => (
  <div className="page-container glass-effect">
    <div className="page-header">
      <h1 className="page-title">{title}</h1>
      <p className="page-subtitle">هذه الصفحة متاحة للعرض في وضع المعاينة.</p>
    </div>
    <div className="empty-state-visual glass-card highlight-orange">
      <span className="material-icons large-icon">find_in_page</span>
      <p>جاري تحميل البيانات التجريبية للمراجعة...</p>
    </div>
  </div>
);

function App() {
  const [page, setPage] = React.useState('doctor-dashboard');

  const renderContent = () => {
    switch (page) {
      case 'doctor-dashboard': return <DoctorDashboard onNavigate={setPage} />;
      case 'review-requests': return <PlaceholderPage title="مراجعة الطلبات" />;
      default: return <DoctorDashboard onNavigate={setPage} />;
    }
  };

  return (
    <div className="app-layout doctor-layout" dir="rtl">
      <Navbar currentPage={page} onNavigate={setPage} />
      <main className="main-content-wrapper">
        {renderContent()}
      </main>
    </div>
  );
}

const root = ReactDOM.createRoot(document.getElementById('root'));
root.render(<App />);
