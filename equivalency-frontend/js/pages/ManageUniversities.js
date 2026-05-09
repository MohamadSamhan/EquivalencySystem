// Manage Universities Page (Admin)
// Add, edit, and view universities

function ManageUniversitiesPage({ onNavigate }) {
  const [universities, setUniversities] = React.useState([]);
  const [loading, setLoading] = React.useState(true);
  const [showForm, setShowForm] = React.useState(false);
  const [editId, setEditId] = React.useState(null);
  const [formData, setFormData] = React.useState({ name: '', location: '' });
  const [saving, setSaving] = React.useState(false);
  const [success, setSuccess] = React.useState('');
  const [error, setError] = React.useState('');

  React.useEffect(() => { loadData(); }, []);

  const loadData = async () => {
    try {
      const res = await adminUniversitiesAPI.getAll();
      setUniversities(Array.isArray(res.data) ? res.data : []);
    } catch (err) {
      console.error('Error loading universities:', err);
    } finally {
      setLoading(false);
    }
  };

  const resetForm = () => {
    setFormData({ name: '', location: '' });
    setEditId(null);
    setShowForm(false);
    setError('');
  };

  const openEdit = (uni) => {
    setFormData({ name: uni.name || '', location: uni.location || '' });
    setEditId(uni.id);
    setShowForm(true);
    setError('');
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.name.trim()) { setError('يرجى إدخال اسم الجامعة.'); return; }
    setSaving(true);
    setError('');
    try {
      if (editId) {
        await adminUniversitiesAPI.update(editId, formData);
        setSuccess('تم تحديث الجامعة بنجاح! ✅');
      } else {
        await adminUniversitiesAPI.create(formData);
        setSuccess('تم إضافة الجامعة بنجاح! 🎉');
      }
      resetForm();
      loadData();
      setTimeout(() => setSuccess(''), 3000);
    } catch (err) {
      setError(err.response?.data?.message || 'فشل في حفظ الجامعة.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="main-content" dir="rtl">
      <div className="page-header">
        <h1 className="page-title">إدارة الجامعات</h1>
        <p className="page-subtitle">إضافة وتعديل الجامعات المسجلة في النظام.</p>
      </div>

      {success && <div className="alert alert-success"><span>✅</span> {success}</div>}

      <div style={{ marginBottom: 20 }}>
        <button className="btn btn-primary" onClick={() => { resetForm(); setShowForm(true); }}>
          ➕ إضافة جامعة جديدة
        </button>
      </div>

      {/* Form Modal */}
      {showForm && (
        <div className="form-card" style={{ marginBottom: 24 }}>
          <h2 className="card-title">{editId ? 'تعديل الجامعة' : 'إضافة جامعة جديدة'}</h2>
          {error && <div className="alert alert-error"><span>⚠️</span> {error}</div>}
          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label className="form-label">اسم الجامعة *</label>
              <input type="text" className="form-input" placeholder="مثال: جامعة اليرموك"
                value={formData.name} onChange={(e) => setFormData({...formData, name: e.target.value})} />
            </div>
            <div className="form-group">
              <label className="form-label">الموقع</label>
              <input type="text" className="form-input" placeholder="مثال: إربد، الأردن"
                value={formData.location} onChange={(e) => setFormData({...formData, location: e.target.value})} />
            </div>
            <div style={{ display: 'flex', gap: 10, marginTop: 16 }}>
              <button type="submit" className="btn btn-primary" disabled={saving}>
                {saving ? 'جاري الحفظ...' : editId ? '💾 تحديث' : '➕ إضافة'}
              </button>
              <button type="button" className="btn btn-outline" onClick={resetForm}>إلغاء</button>
            </div>
          </form>
        </div>
      )}

      {/* Table */}
      <div className="table-container">
        <div className="table-header">
          <div className="table-title">الجامعات ({universities.length})</div>
        </div>
        {loading ? (
          <div className="loading-screen"><div className="spinner"></div><span>جاري التحميل...</span></div>
        ) : universities.length === 0 ? (
          <div className="empty-state">
            <div className="empty-state-icon">🏛️</div>
            <div className="empty-state-title">لا توجد جامعات</div>
          </div>
        ) : (
          <div className="table-wrapper">
            <table className="data-table">
              <thead>
                <tr>
                  <th>#</th>
                  <th>اسم الجامعة</th>
                  <th>الموقع</th>
                  <th>الإجراء</th>
                </tr>
              </thead>
              <tbody>
                {universities.map((uni, idx) => (
                  <tr key={uni.id || idx}>
                    <td>{idx + 1}</td>
                    <td style={{ fontWeight: 600 }}>{uni.name}</td>
                    <td>{uni.location || '—'}</td>
                    <td>
                      <button className="btn btn-outline btn-sm" onClick={() => openEdit(uni)}>
                        ✏️ تعديل
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
