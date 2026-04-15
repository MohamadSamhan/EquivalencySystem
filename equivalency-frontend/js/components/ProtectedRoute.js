// ProtectedRoute Component
// Checks if user is authenticated and has the correct role

function ProtectedRoute({ children, allowedRoles }) {
  const { isAuthenticated, role, loading } = useAuth();

  if (loading) return null;

  if (!isAuthenticated) {
    // Redirect to login if not authenticated
    window.location.hash = '#/login';
    return null;
  }

  if (allowedRoles && !allowedRoles.includes(role)) {
    return (
      <div className="main-content" dir="rtl">
        <div className="empty-state">
          <div className="empty-state-icon">🔒</div>
          <div className="empty-state-title">تم رفض الوصول</div>
          <div className="empty-state-desc">
            ليس لديك الصلاحية للوصول إلى هذه الصفحة.
          </div>
        </div>
      </div>
    );
  }

  return children;
}
