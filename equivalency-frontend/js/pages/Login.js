// Login Page Component
// Arabic university-style login matching the SIS design

function LoginPage() {
  const { login } = useAuth();
  const [email, setEmail] = React.useState('');
  const [password, setPassword] = React.useState('');
  const [remember, setRemember] = React.useState(false);
  const [loading, setLoading] = React.useState(false);
  const [error, setError] = React.useState('');

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');

    if (!email || !password) {
      setError('يرجى إدخال الرقم الجامعي أو البريد الإلكتروني وكلمة المرور');
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

      // Navigation is now handled by the role returned from login
      const defaultPage = result.role === 'Doctor' ? 'doctor-dashboard' : 'student-dashboard';
      window.location.hash = '#/' + defaultPage;

    } catch (err) {
      console.error('Login error:', err);
      if (err.response) {
        setError(err.response.data?.message || 'الرقم الجامعي أو كلمة المرور غير صحيحة');
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
          <p className="login-info-text">سجّل دخولك للمتابعة</p>
        </div>

        {error && (
          <div className="alert alert-error" style={{ marginBottom: 20 }}>
            <span>⚠️</span> {error}
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <div className="sis-form-group">
            <input
              id="login-email"
              className="sis-input"
              type="email"
              placeholder="الإيميل الجامعي"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              autoComplete="email"
              dir="ltr"
              style={{ textAlign: 'right', paddingLeft: 15, paddingRight: 45 }}
            />
            <span className="sis-input-icon">👤</span>
          </div>

          <div className="sis-form-group">
            <input
              id="login-password"
              className="sis-input"
              type="password"
              placeholder="كلمة المرور"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              autoComplete="current-password"
            />
            <span className="sis-input-icon">🔑</span>
          </div>

          <div className="form-footer-row" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
            <label className="sis-checkbox-container" style={{ margin: 0 }}>
              الاحتفاظ بإسم المستخدم
              <input
                type="checkbox"
                className="sis-checkbox"
                checked={remember}
                onChange={(e) => setRemember(e.target.checked)}
              />
            </label>
           
          </div>

          <button
            type="submit"
            className="sis-submit-btn"
            disabled={loading}
          >
            {loading ? (
              <>
                <span className="spinner"></span>
                جاري الدخول...
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
