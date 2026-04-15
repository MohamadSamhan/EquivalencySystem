// Login Page Component
// Arabic university-style login matching the SIS design

function LoginPage() {
  const { login } = useAuth();
  const [email, setEmail] = React.useState('');
  const [password, setPassword] = React.useState('');
  const [showPassword, setShowPassword] = React.useState(false);
  const [remember, setRemember] = React.useState(false);
  const [loading, setLoading] = React.useState(false);
  const [error, setError] = React.useState('');

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');

    if (!email || !password) {
      setError('يرجى إدخال البريد الإلكتروني وكلمة المرور');
      return;
    }

    setLoading(true);
    try {
      const result = await login(email, password);

      if (remember) {
        localStorage.setItem('rememberedUser', email);
      } else {
        localStorage.removeItem('rememberedUser');
      }

      const defaultPage = result.role === 'Doctor' ? 'doctor-dashboard' : 'student-dashboard';
      window.location.hash = '#/' + defaultPage;

    } catch (err) {
      console.error('Login error:', err);
      if (err.response) {
        setError(err.response.data?.message || 'البريد الإلكتروني أو كلمة المرور غير صحيحة');
      } else {
        setError('لا يمكن الاتصال بالخادم. يرجى التأكد من تشغيل الـ API.');
      }
    } finally {
      setLoading(false);
    }
  };

  React.useEffect(() => {
    const saved = localStorage.getItem('rememberedUser');
    if (saved) {
      setEmail(saved);
      setRemember(true);
    }
  }, []);

  return (
    <div className="login-page" dir="rtl">
      <div className="login-card">
        <div className="login-header">
          <div className="login-logo-wrap">
            <div className="login-logo-bg">
              <img
                src="https://tse3.mm.bing.net/th/id/OIP.PTZBRYYX0-Mv83-mVEpfWgAAAA?rs=1&pid=ImgDetMain&o=7&rm=3"
                alt="University Logo"
                className="login-logo-img"
              />
            </div>
          </div>
          <h1 className="login-title">نظام معادلة المواد الجامعية</h1>
          <p className="login-info-text">سجّل دخولك للمتابعة في النظام</p>
        </div>

        {error && (
          <div className="alert alert-error">
             {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="login-form">
          <div className="form-group">
            <label className="form-label" htmlFor="login-email">البريد الإلكتروني الجامعي</label>
            <div className="input-wrapper">
              <input
                id="login-email"
                type="email"
                placeholder="example@univ.edu.jo"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                autoComplete="email"
                dir="ltr"
              />
              <span className="material-icons input-icon">person</span>
            </div>
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="login-password">كلمة المرور</label>
            <div className="input-wrapper">
              <input
                id="login-password"
                type={showPassword ? "text" : "password"}
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                autoComplete="current-password"
              />
              <span className="material-icons input-icon">lock</span>
              <button 
                type="button" 
                className="password-toggle"
                onClick={() => setShowPassword(!showPassword)}
                aria-label={showPassword ? "إخفاء كلمة المرور" : "إظهار كلمة المرور"}
              >
                <span className="material-icons" style={{ fontSize: '20px' }}>
                  {showPassword ? "visibility_off" : "visibility"}
                </span>
              </button>
            </div>
          </div>

          <div className="form-footer-actions">
            <label className="checkbox-container">
              <input
                type="checkbox"
                checked={remember}
                onChange={(e) => setRemember(e.target.checked)}
              />
              <span>تذكرني على هذا الجهاز</span>
            </label>
            <a href="#" className="forgot-password-link">نسيت كلمة المرور؟</a>
          </div>

          <button
            type="submit"
            className="login-btn"
            disabled={loading}
          >
            {loading ? (
              <>
                <span className="spinner"></span>
                جاري التحقق...
              </>
            ) : (
              <>تسجيل الدخول</>
            )}
          </button>
        </form>
      </div>
    </div>
  );
}
