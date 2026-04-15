// Navbar Component - Dynamic Sidebar Navigation
// Changes links based on user role (Student / Doctor)

function Navbar({ currentPage, onNavigate }) {
  const { role, user, logout } = useAuth();
  const rawName = user?.name || localStorage.getItem('userName') || localStorage.getItem('fullName') || '';
  const userName = rawName ? rawName.split(' ')[0] : (role === 'Doctor' ? 'دكتور' : 'طالب');
  const [mobileOpen, setMobileOpen] = React.useState(false);
  const [theme, setTheme] = React.useState(localStorage.getItem('theme') || 'dark');

  React.useEffect(() => {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem('theme', theme);
  }, [theme]);

  const toggleTheme = () => setTheme(prev => prev === 'dark' ? 'light' : 'dark');

  const studentLinks = [
    { id: 'student-dashboard', label: 'لوحة التحكم', icon: '🏠' },
    { id: 'add-course', label: 'إضافة مساق', icon: '📚' },
    { id: 'my-courses', label: 'مساقاتي', icon: '📋' },
    { id: 'requests', label: 'طلب معادلة', icon: '🔄' },
    { id: 'results', label: 'عرض النتائج', icon: '📊' },
  ];

  const doctorLinks = [
    { id: 'doctor-dashboard', label: 'لوحة التحكم', icon: '🏠' },
    { id: 'review-requests', label: 'مراجعة الطلبات', icon: '📝' },
  ];

  const links = role === 'Doctor' ? doctorLinks : studentLinks;

  const handleNav = (pageId) => {
    onNavigate(pageId);
    setMobileOpen(false);
  };

  return (
    <>
      {/* Mobile Header */}
      <div className="mobile-header" dir="rtl">
        <button className="mobile-menu-btn" onClick={() => setMobileOpen(!mobileOpen)}>
          ☰
        </button>
        <span style={{ fontWeight: 700, fontSize: 15 }}>نظام معادلة المواد</span>
      </div>

      {/* Overlay for mobile */}
      {mobileOpen && (
        <div className="sidebar-overlay" onClick={() => setMobileOpen(false)} />
      )}

      {/* Sidebar */}
      <nav className={`sidebar ${mobileOpen ? 'open' : ''}`} dir="rtl">
        <div className="sidebar-header">
          <div className="sidebar-brand">
            <div className="sidebar-brand-icon">
              <img src="https://tse3.mm.bing.net/th/id/OIP.PTZBRYYX0-Mv83-mVEpfWgAAAA?rs=1&pid=ImgDetMain&o=7&rm=3" alt="شعار جامعة اليرموك" style={{ width: '40px', height: '40px', objectFit: 'contain', borderRadius: '8px' }} />
            </div>
            <div>
              <div className="sidebar-brand-text">معادلة المواد</div>
              <div className="sidebar-brand-sub">نظام مدعوم بالذكاء الاصطناعي</div>
            </div>
          </div>
        </div>

        <div className="sidebar-nav">
          <div className="sidebar-section-label">التنقل</div>
          {links.map((link) => (
            <button
              key={link.id}
              className={`sidebar-link ${currentPage === link.id ? 'active' : ''}`}
              onClick={() => handleNav(link.id)}
            >
              <span className="sidebar-link-icon">{link.icon}</span>
              {link.label}
            </button>
          ))}
        </div>

        <div className="sidebar-footer">
          <div className="sidebar-user" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
              <div className="sidebar-user-avatar">
                {userName.charAt(0).toUpperCase()}
              </div>
              <div className="sidebar-user-info">
                <div className="sidebar-user-name">{userName}</div>
                <div className="sidebar-user-role">{role === 'Doctor' ? 'دكتور' : 'طالب'}</div>
              </div>
            </div>
            <button 
              onClick={toggleTheme} 
              style={{
                background: 'transparent', border: 'none', cursor: 'pointer',
                fontSize: '20px', color: 'var(--text-secondary)', padding: '4px'
              }}
              title="تغيير المظهر"
            >
              {theme === 'dark' ? '☀️' : '🌙'}
            </button>
          </div>
          <button className="logout-btn" onClick={logout}>
            <span className="sidebar-link-icon">🚪</span>
            تسجيل الخروج
          </button>
        </div>
      </nav>
    </>
  );
}
