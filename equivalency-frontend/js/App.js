// App.js - Main Controller
// Handles Routing, Authentication Context, and Sidebar Layout

function AppContent() {
  const { isAuthenticated, loading, role } = useAuth();
  const [currentPage, setCurrentPage] = React.useState('');

  // Handle URL hash changes for routing
  React.useEffect(() => {
    const handleHashChange = () => {
      const hash = window.location.hash.replace('#/', '') || '';
      
      if (!isAuthenticated && !loading) {
        setCurrentPage('login');
      } else if (isAuthenticated && (!hash || hash === 'login')) {
        // Redirect to default dashboard if authenticated but on login or empty hash
        const defaultPage = role === 'Doctor' ? 'doctor-dashboard' : 'student-dashboard';
        navigate(defaultPage);
      } else {
        setCurrentPage(hash);
      }
    };

    window.addEventListener('hashchange', handleHashChange);
    handleHashChange(); // Initial check

    return () => window.removeEventListener('hashchange', handleHashChange);
  }, [isAuthenticated, loading, role]);

  const navigate = (pageId) => {
    window.location.hash = `#/${pageId}`;
  };

  if (loading) {
    return (
      <div className="loading-screen" dir="rtl">
        <div className="spinner"></div>
        <p>جاري تحميل الجلسة...</p>
      </div>
    );
  }

  // Auth pages (no sidebar)
  if (!isAuthenticated || currentPage === 'login') {
    return <LoginPage onNavigate={navigate} />;
  }

  // Sidebar Layout for Authenticated Users
  return (
    <div className="app-container" dir="rtl">
      <Navbar currentPage={currentPage} onNavigate={navigate} />
      
      <main className="main-wrapper">
        <div className="content-area">
          <RenderPage pageId={currentPage} onNavigate={navigate} role={role} />
        </div>
      </main>
    </div>
  );
}

// Route Resolver
function RenderPage({ pageId, onNavigate, role }) {
  switch (pageId) {
    // Shared / Dashboard
    case 'student-dashboard':
      return <ProtectedRoute allowedRoles={['Student']}><StudentDashboard onNavigate={onNavigate} /></ProtectedRoute>;
    case 'doctor-dashboard':
      return <ProtectedRoute allowedRoles={['Doctor']}><DoctorDashboard onNavigate={onNavigate} /></ProtectedRoute>;

    // Student Pages
    case 'add-course':
      return <ProtectedRoute allowedRoles={['Student']}><AddCoursePage onNavigate={onNavigate} /></ProtectedRoute>;
    case 'my-courses':
      return <ProtectedRoute allowedRoles={['Student']}><StudentCoursesPage onNavigate={onNavigate} /></ProtectedRoute>;
    case 'requests':
      return <ProtectedRoute allowedRoles={['Student']}><RequestsPage onNavigate={onNavigate} /></ProtectedRoute>;
    case 'results':
      return <ProtectedRoute allowedRoles={['Student']}><ViewResultsPage onNavigate={onNavigate} /></ProtectedRoute>;

    // Doctor Pages
    case 'review-requests':
      return <ProtectedRoute allowedRoles={['Doctor']}><ReviewRequestsPage onNavigate={onNavigate} /></ProtectedRoute>;

    // Fallbacks
    case '':
      return <div className="main-content" dir="rtl">جاري التوجيه...</div>;
    default:
      return (
        <div className="main-content" style={{ textAlign: 'center', paddingTop: 100 }} dir="rtl">
          <h1 style={{ fontSize: 64, color: '#334155' }}>404</h1>
          <h2 style={{ color: '#94a3b8' }}>الصفحة غير موجودة</h2>
          <button 
            className="btn btn-primary" 
            style={{ marginTop: 24 }}
            onClick={() => onNavigate(role === 'Doctor' ? 'doctor-dashboard' : 'student-dashboard')}
          >
            العودة للرئيسية
          </button>
        </div>
      );
  }
}

function App() {
  return (
    <AuthProvider>
      <AppContent />
    </AuthProvider>
  );
}

// Render the application
const root = ReactDOM.createRoot(document.getElementById('root'));
root.render(<App />);
