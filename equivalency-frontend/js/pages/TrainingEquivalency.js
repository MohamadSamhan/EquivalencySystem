// Training Certificate Equivalency Page (Student)
// Submit training certificate equivalency requests and track their status

function TrainingEquivalencyPage({ onNavigate }) {
  const [activeTab, setActiveTab] = React.useState('submit'); // 'submit' | 'track'

  // Form state
  const [trainingTitle, setTrainingTitle] = React.useState('');
  const [trainingProvider, setTrainingProvider] = React.useState('');
  const [trainingHours, setTrainingHours] = React.useState('');
  const [pdfFile, setPdfFile] = React.useState(null);
  const [loading, setLoading] = React.useState(false);
  const [success, setSuccess] = React.useState('');
  const [error, setError] = React.useState('');

  // Track state
  const [requests, setRequests] = React.useState([]);
  const [trackLoading, setTrackLoading] = React.useState(false);
  const [trackFilter, setTrackFilter] = React.useState('الكل');

  const handleFileChange = (e) => {
    const file = e.target.files[0];
    if (file && file.type === 'application/pdf') {
      setPdfFile(file);
      setError('');
    } else if (file) {
      setError('يرجى اختيار ملف PDF فقط.');
      setPdfFile(null);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setSuccess('');

    if (!trainingTitle || !trainingProvider || !trainingHours) {
      setError('يرجى ملء جميع الحقول المطلوبة.');
      return;
    }

    if (!pdfFile) {
      setError('يرجى رفع نسخة PDF من شهادة التدريب.');
      return;
    }

    setLoading(true);
    try {
      const token = localStorage.getItem('token');
      const form = new FormData();
      form.append('trainingTitle', trainingTitle);
      form.append('trainingProvider', trainingProvider);
      form.append('trainingHours', trainingHours);
      form.append('certificateFile', pdfFile);

      await axios.post('https://localhost:7012/api/training-requests', form, {
        headers: {
          'Content-Type': 'multipart/form-data',
          Authorization: `Bearer ${token}`
        }
      });

      setSuccess('تم تقديم طلب معادلة الشهادة التدريبية بنجاح! 🎉');
      setTrainingTitle('');
      setTrainingProvider('');
      setTrainingHours('');
      setPdfFile(null);
      // Reset file input
      const fileInput = document.getElementById('training-cert-file');
      if (fileInput) fileInput.value = '';
      
      setTimeout(() => setSuccess(''), 4000);
    } catch (err) {
      setError(err.response?.data?.message || 'فشل في تقديم الطلب. يرجى المحاولة مرة أخرى.');
    } finally {
      setLoading(false);
    }
  };

  const loadRequests = async () => {
    setTrackLoading(true);
    try {
      const res = await trainingAPI.getMyRequests();
      setRequests(Array.isArray(res.data) ? res.data : []);
    } catch (err) {
      console.error('Error loading training requests:', err);
    } finally {
      setTrackLoading(false);
    }
  };

  React.useEffect(() => {
    if (activeTab === 'track') {
      loadRequests();
    }
  }, [activeTab]);

  const getStatusString = (statusCode) => {
    if (statusCode === 0 || statusCode === 'Pending') return 'Pending';
    if (statusCode === 1 || statusCode === 'Approved') return 'Approved';
    if (statusCode === 2 || statusCode === 'Rejected') return 'Rejected';
    return String(statusCode);
  };

  const getStatusText = (status) => {
    const s = getStatusString(status);
    switch (s) {
      case 'Approved': return 'تمت الموافقة';
      case 'Rejected': return 'تم الرفض';
      default: return 'قيد الانتظار';
    }
  };

  const getBadgeClass = (status) => {
    const s = getStatusString(status);
    switch (s) {
      case 'Approved': return 'badge-approved';
      case 'Rejected': return 'badge-rejected';
      default: return 'badge-pending';
    }
  };

  const filteredRequests = trackFilter === 'الكل'
    ? requests
    : requests.filter(r => {
        const s = getStatusString(r.status);
        if (trackFilter === 'قيد الانتظار') return s === 'Pending';
        if (trackFilter === 'تمت الموافقة') return s === 'Approved';
        if (trackFilter === 'تم الرفض') return s === 'Rejected';
        return true;
      });

  return (
    <div className="main-content" dir="rtl">
      <div className="page-header">
        <h1 className="page-title">معادلة الشهادات التدريبية</h1>
        <p className="page-subtitle">قدّم طلب معادلة لشهاداتك التدريبية وتابع حالتها.</p>
      </div>

      {/* Tab Switcher */}
      <div className="filter-tabs" style={{ marginBottom: 24 }}>
        <button
          className={`btn btn-sm ${activeTab === 'submit' ? 'btn-primary' : 'btn-outline'}`}
          onClick={() => setActiveTab('submit')}
          style={{ animationName: 'none' }}
        >
          📝 تقديم طلب جديد
        </button>
        <button
          className={`btn btn-sm ${activeTab === 'track' ? 'btn-primary' : 'btn-outline'}`}
          onClick={() => setActiveTab('track')}
          style={{ animationName: 'none' }}
        >
          📊 متابعة الطلبات
          {requests.length > 0 && <span style={{ marginRight: 6, opacity: 0.7 }}>({requests.length})</span>}
        </button>
      </div>

      {/* ========== Submit Tab ========== */}
      {activeTab === 'submit' && (
        <>
          {success && (
            <div className="alert alert-success">
              <span>✅</span> {success}
            </div>
          )}
          {error && (
            <div className="alert alert-error">
              <span>⚠️</span> {error}
            </div>
          )}

          <div className="add-course-layout training-submit-layout">
            <div className="form-card" style={{ maxWidth: '100%' }}>
              <h2 className="card-title">معلومات الشهادة التدريبية</h2>

              <form onSubmit={handleSubmit}>
                <div className="form-group">
                  <label className="form-label">عنوان التدريب *</label>
                  <input
                    type="text"
                    className="form-input"
                    placeholder="مثال: دورة تطوير تطبيقات الويب"
                    value={trainingTitle}
                    onChange={(e) => setTrainingTitle(e.target.value)}
                  />
                </div>

                <div className="form-group">
                  <label className="form-label">الجهة المقدمة للتدريب *</label>
                  <input
                    type="text"
                    className="form-input"
                    placeholder="مثال: أكاديمية الإبداع التقني"
                    value={trainingProvider}
                    onChange={(e) => setTrainingProvider(e.target.value)}
                  />
                </div>

                <div className="form-group">
                  <label className="form-label">عدد ساعات التدريب *</label>
                  <input
                    type="number"
                    className="form-input"
                    placeholder="مثال: 40"
                    min="1"
                    value={trainingHours}
                    onChange={(e) => setTrainingHours(e.target.value)}
                  />
                </div>

                <div className="form-group">
                  <label className="form-label">شهادة التدريب (PDF) *</label>
                  <div
                    className="file-upload-zone"
                    onClick={() => document.getElementById('training-cert-file').click()}
                  >
                    <input
                      type="file"
                      id="training-cert-file"
                      accept="application/pdf"
                      style={{ display: 'none' }}
                      onChange={handleFileChange}
                    />
                    {pdfFile ? (
                      <div style={{ textAlign: 'center' }}>
                        <span style={{ fontSize: 28 }}>📄</span>
                        <p style={{ marginTop: 8, fontWeight: 600 }}>{pdfFile.name}</p>
                        <p style={{ fontSize: 12, color: '#94a3b8' }}>
                          {(pdfFile.size / 1024 / 1024).toFixed(2)} MB
                        </p>
                      </div>
                    ) : (
                      <div style={{ textAlign: 'center' }}>
                        <span style={{ fontSize: 32 }}>📤</span>
                        <p style={{ marginTop: 8, fontWeight: 500 }}>اضغط لرفع شهادة التدريب</p>
                        <p style={{ fontSize: 12, color: '#94a3b8' }}>PDF فقط — الحد الأقصى 10 ميغابايت</p>
                      </div>
                    )}
                  </div>
                </div>

                <button
                  type="submit"
                  className="btn btn-primary"
                  disabled={loading}
                  style={{ width: '100%', marginTop: 8 }}
                >
                  {loading ? (
                    <><span className="spinner"></span> جاري الإرسال...</>
                  ) : (
                    '📨 تقديم الطلب'
                  )}
                </button>
              </form>
            </div>

            {/* Tips */}
            <div className="add-course-sidebar-card">
              <h3>💡 نصائح مهمة</h3>
              <div className="tips-grid training-tips-grid">
                <div className="tip-item training-tip-item"><span className="tip-icon">📋</span><span>تأكد من كتابة عنوان التدريب كما يظهر في الشهادة</span></div>
                <div className="tip-item training-tip-item"><span className="tip-icon">🏢</span><span>اذكر اسم الجهة المقدمة كاملاً وبشكل صحيح</span></div>
                <div className="tip-item training-tip-item"><span className="tip-icon">⏱️</span><span>أدخل عدد الساعات كما هو مذكور في الشهادة</span></div>
                <div className="tip-item training-tip-item"><span className="tip-icon">📄</span><span>ارفع نسخة واضحة بصيغة PDF من الشهادة</span></div>
              </div>
            </div>
          </div>
        </>
      )}

      {/* ========== Track Tab ========== */}
      {activeTab === 'track' && (
        <>
          {/* Filter Tabs */}
          <div className="filter-tabs" style={{ marginBottom: 16 }}>
            {['الكل', 'قيد الانتظار', 'تمت الموافقة', 'تم الرفض'].map((tab) => (
              <button
                key={tab}
                className={`btn btn-sm ${trackFilter === tab ? 'btn-primary' : 'btn-outline'}`}
                onClick={() => setTrackFilter(tab)}
                style={{ animationName: 'none' }}
              >
                {tab}
                <span style={{ marginRight: 6, opacity: 0.7 }}>
                  ({tab === 'الكل' ? requests.length : requests.filter(r => {
                    const s = getStatusString(r.status);
                    if (tab === 'قيد الانتظار') return s === 'Pending';
                    if (tab === 'تمت الموافقة') return s === 'Approved';
                    if (tab === 'تم الرفض') return s === 'Rejected';
                    return false;
                  }).length})
                </span>
              </button>
            ))}
          </div>

          <div className="table-container">
            <div className="table-header">
              <div className="table-title">طلبات معادلة الشهادات التدريبية ({filteredRequests.length})</div>
            </div>

            {trackLoading ? (
              <div className="loading-screen">
                <div className="spinner"></div>
                <span>جاري تحميل الطلبات...</span>
              </div>
            ) : filteredRequests.length === 0 ? (
              <div className="empty-state">
                <div className="empty-state-icon">🎓</div>
                <div className="empty-state-title">لا توجد طلبات</div>
                <div className="empty-state-desc">لم تقدّم أي طلب معادلة شهادة تدريبية بعد.</div>
                <button className="btn btn-primary" style={{ marginTop: 16 }} onClick={() => setActiveTab('submit')}>
                  📝 تقديم طلب جديد
                </button>
              </div>
            ) : (
              <div className="table-wrapper">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>#</th>
                      <th>عنوان التدريب</th>
                      <th>الجهة المقدمة</th>
                      <th>عدد الساعات</th>
                      <th>تاريخ التقديم</th>
                      <th>الحالة</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filteredRequests.map((req, idx) => (
                      <tr key={req.id || idx}>
                        <td>{idx + 1}</td>
                        <td style={{ fontWeight: 600 }}>{req.trainingTitle || '—'}</td>
                        <td>{req.trainingProvider || '—'}</td>
                        <td>{req.trainingHours || '—'} ساعة</td>
                        <td style={{ fontSize: 13 }}>
                          {req.createdAt ? new Date(req.createdAt).toLocaleDateString('ar-JO') : '—'}
                        </td>
                        <td>
                          <span className={`badge ${getBadgeClass(req.status)}`}>
                            <span className="badge-dot"></span>
                            {getStatusText(req.status)}
                          </span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </>
      )}
    </div>
  );
}
