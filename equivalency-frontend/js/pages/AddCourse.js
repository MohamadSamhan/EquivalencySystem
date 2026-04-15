// Add Course Page (Student)
// Form to add previously completed courses with university dropdown

function AddCoursePage({ onNavigate }) {
  const [courseName, setCourseName] = React.useState('');
  const [creditHours, setCreditHours] = React.useState('');
  const [description, setDescription] = React.useState('');
  const [universityId, setUniversityId] = React.useState('');
  const [pdfFile, setPdfFile] = React.useState(null);
  const [universities, setUniversities] = React.useState([]);
  const [loading, setLoading] = React.useState(false);
  const [loadingUnis, setLoadingUnis] = React.useState(true);
  const [success, setSuccess] = React.useState('');
  const [error, setError] = React.useState('');

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

  React.useEffect(() => {
    loadUniversities();
  }, []);

  const loadUniversities = async () => {
    try {
      const res = await universitiesAPI.getAll();
      setUniversities(Array.isArray(res.data) ? res.data : []);
    } catch (err) {
      console.error('Failed to load universities:', err);
      // Fallback demo data
      setUniversities([
        { id: 1, name: 'جامعة اليرموك' },
        { id: 2, name: 'الجامعة الأردنية' },
        { id: 3, name: 'جامعة العلوم والتكنولوجيا' },
        { id: 4, name: 'جامعة مؤتة' },
        { id: 5, name: 'الجامعة الهاشمية' },
      ]);
    } finally {
      setLoadingUnis(false);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setSuccess('');

    if (!courseName || !creditHours || !universityId) {
      setError('يرجى ملء جميع الحقول المطلوبة.');
      return;
    }

    setLoading(true);
    try {
      const token = localStorage.getItem('token');
      const form = new FormData();
      form.append("courseName", courseName);
      form.append("creditHours", creditHours);
      form.append("description", description);
      form.append("universityId", universityId);
      if (pdfFile) {
        form.append("studentFile", pdfFile); // <-- اسم المفتاح لازم يطابق studentFile
      }

      await axios.post("https://localhost:7012/api/studentcourses", form, {
        headers: {
          "Content-Type": "multipart/form-data",
          Authorization: `Bearer ${token}`
        }
      });

      setSuccess('تم إضافة المساق بنجاح! 🎉');
      setCourseName('');
      setCreditHours('');
      setDescription('');
      setUniversityId('');
      setPdfFile(null);

      // Clear success after 4 seconds
      setTimeout(() => setSuccess(''), 4000);
    } catch (err) {
      setError(err.response?.data?.message || 'فشل في إضافة المساق. يرجى المحاولة مرة أخرى.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="main-content" dir="rtl">
      <div className="page-header">
        <h1 className="page-title">إضافة مساق</h1>
        <p className="page-subtitle">أضف المساقات التي أكملتها في جامعتك السابقة لتتمكن من تقديم طلب معادلة.</p>
      </div>

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

      <div className="add-course-layout">
        {/* Main Form Card */}
        <div className="form-card" style={{ maxWidth: '100%' }}>
          <h2 className="card-title">معلومات المساق</h2>

          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label className="form-label" htmlFor="course-name">اسم المساق <span style={{ color: 'var(--danger-500)' }}>*</span></label>
              <input
                id="course-name"
                type="text"
                placeholder="مثال: مقدمة في علوم الحاسوب"
                value={courseName}
                onChange={(e) => setCourseName(e.target.value)}
              />
            </div>

            <div className="form-row">
              <div className="form-group">
                <label className="form-label" htmlFor="credit-hours">عدد الساعات <span style={{ color: 'var(--danger-500)' }}>*</span></label>
                <input
                  id="credit-hours"
                  type="number"
                  min="1"
                  max="6"
                  placeholder="مثال: 3"
                  value={creditHours}
                  onChange={(e) => setCreditHours(e.target.value)}
                />
              </div>

              <div className="form-group">
                <label className="form-label" htmlFor="university-select">الجامعة <span style={{ color: 'var(--danger-500)' }}>*</span></label>
                <select
                  id="university-select"
                  value={universityId}
                  onChange={(e) => setUniversityId(e.target.value)}
                  disabled={loadingUnis}
                >
                  <option value="">
                    {loadingUnis ? 'جاري تحميل الجامعات...' : 'اختر الجامعة'}
                  </option>
                  {universities.map((uni) => (
                    <option key={uni.id} value={uni.id}>
                      {uni.name}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="course-desc">وصف المساق</label>
              <textarea
                id="course-desc"
                placeholder="صف محتوى المساق والمواضيع التي تمت تغطيتها باختصار..."
                value={description}
                onChange={(e) => setDescription(e.target.value)}
              />
            </div>

            <div className="form-group">
              <label className="form-label">وصف المساق (PDF) لجهاز المقارنة <span style={{ color: 'var(--danger-500)' }}>*</span></label>
              <div
                className={`file-upload-zone${pdfFile ? ' has-file' : ''}`}
                onClick={() => document.getElementById('pdf-upload').click()}
              >
                <input
                  id="pdf-upload"
                  type="file"
                  accept=".pdf"
                  style={{ display: 'none' }}
                  onChange={handleFileChange}
                />
                <span className="file-upload-icon">{pdfFile ? '✅' : '📄'}</span>
                <div className="file-upload-text">
                  {pdfFile ? `تم اختيار: ${pdfFile.name}` : 'اضغط هنا لرفع ملف PDF الخاص بوصف المساق'}
                </div>
                {pdfFile && (
                  <button
                    type="button"
                    className="file-remove-btn"
                    onClick={(e) => { e.stopPropagation(); setPdfFile(null); }}
                  >
                    إزالة الملف
                  </button>
                )}
              </div>
              <p className="file-hint">سيتم استخدام هذا الملف لمقارنة المحتوى العلمي وحساب نسبة التشابه.</p>
            </div>

            <div className="form-actions">
              <button
                type="submit"
                className="btn btn-primary"
                disabled={loading}
                id="add-course-submit"
              >
                {loading ? (
                  <>
                    <span className="spinner"></span>
                    جاري الحفظ...
                  </>
                ) : (
                  <>📚 إضافة المساق</>
                )}
              </button>
              <button
                type="button"
                className="btn btn-outline"
                onClick={() => onNavigate('my-courses')}
              >
                عرض مساقاتي
              </button>
            </div>
          </form>
        </div>

        {/* Tips - Horizontal Grid */}
        <div className="add-course-sidebar-card">
          <h3>💡 نصائح للإضافة الصحيحة</h3>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(180px, 1fr))', gap: '4px' }}>
            <div className="tip-item"><span className="tip-icon">📌</span><span>تأكد من كتابة اسم المساق كما هو مذكور في كشف علاماتك الرسمي.</span></div>
            <div className="tip-item"><span className="tip-icon">⏱️</span><span>أدخل عدد الساعات المعتمدة الصحيح للمساق (عادةً بين 1 و6 ساعات).</span></div>
            <div className="tip-item"><span className="tip-icon">🎓</span><span>اختر الجامعة التي درست فيها هذا المساق بدقة.</span></div>
            <div className="tip-item"><span className="tip-icon">📄</span><span>ارفع ملف PDF لوصف المساق لتحسين دقة المقارنة.</span></div>
            <div className="tip-item"><span className="tip-icon">✍️</span><span>الوصف النصي يساعد النظام على فهم محتوى المساق بشكل أفضل.</span></div>
          </div>
        </div>
      </div>
    </div>
  );
}
