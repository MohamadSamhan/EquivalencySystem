// Manage Departments Page (Admin)
// Add, edit, and view academic departments

function ManageDepartmentsPage({ onNavigate }) {
  const [departments, setDepartments] = React.useState([]);
  const [universities, setUniversities] = React.useState([]);
  const [loading, setLoading] = React.useState(true);
  const [showForm, setShowForm] = React.useState(false);
  const [editId, setEditId] = React.useState(null);
  const [formData, setFormData] = React.useState({ name: '', universityId: '' });
  const [saving, setSaving] = React.useState(false);
  const [success, setSuccess] = React.useState('');
  const [error, setError] = React.useState('');

  React.useEffect(() => { loadData(); }, []);

  const loadData = async () => {
    try {
      const [dRes, uRes] = await Promise.all([
        adminDepartmentsAPI.getAll(),
        adminUniversitiesAPI.getAll().catch(() => ({ data: [] })),
      ]);
      setDepartments(Array.isArray(dRes.data) ? dRes.data : []);
      setUniversities(Array.isArray(uRes.data) ? uRes.data : []);
    } catch (err) {
      console.error('Error loading departments:', err);
    } finally {
      setLoading(false);
    }
  };

  const resetForm = () => {
    setFormData({ name: '', universityId: '' });
    setEditId(null);
    setShowForm(false);
    setError('');
  };

  const openEdit = (dept) => {
    setFormData({ name: dept.name || '', universityId: dept.universityId || '' });
    setEditId(dept.id);
    setShowForm(true);
    setError('');
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.name.trim()) { setError('يرجى إدخال اسم القسم.'); return; }
    setSaving(true);
    setError('');
    try {
      if (editId) {
        await adminDepartmentsAPI.update(editId, formData);
        setSuccess('تم تحديث القسم بنجاح! ✅');
      } else {
        await adminDepartmentsAPI.create(formData);
        setSuccess('تم إضافة القسم بنجاح! 🎉');
      }
      resetForm();
      loadData();
      setTimeout(() => setSuccess(''), 3000);
    } catch (err) {
      setError(err.response?.data?.message || 'فشل في حفظ القسم.');
    } finally {
      setSaving(false);
    }
  };

  const getUniName = (id) => { const u = universities.find(u => u.id === id); return u ? u.name : '—'; };

  return (
    <div className="main-content" dir="rtl">
      <div className="page-header">
        <h1 className="page-title">إدارة الأقسام</h1>
        <p className="page-subtitle">إضافة وتعديل الأقسام الأكاديمية في النظام.</p>
      </div>

      {success && <div className="alert alert-success"><span>✅</span> {success}</div>}

      <div style={{ marginBottom: 20 }}>
        <button className="btn btn-primary" onClick={() => { resetForm(); setShowForm(true); }}>
          ➕ إضافة قسم جديد
        </button>
      </div>

      {showForm && (
        <div className="form-card" style={{ marginBottom: 24 }}>
          <h2 className="card-title">{editId ? 'تعديل القسم' : 'إضافة قسم جديد'}</h2>
          {error && <div className="alert alert-error"><span>⚠️</span> {error}</div>}
          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label className="form-label">اسم القسم *</label>
              <input type="text" className="form-input" placeholder="مثال: قسم علوم الحاسوب"
                value={formData.name} onChange={(e) => setFormData({...formData, name: e.target.value})} />
            </div>
            <div className="form-group">
              <label className="form-label">الجامعة</label>
              <select className="form-input" value={formData.universityId}
                onChange={(e) => setFormData({...formData, universityId: e.target.value})}>
                <option value="">— اختر الجامعة —</option>
                {universities.map(u => <option key={u.id} value={u.id}>{u.name}</option>)}
              </select>
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

      <div className="table-container">
        <div className="table-header">
          <div className="table-title">الأقسام ({departments.length})</div>
        </div>
        {loading ? (
          <div className="loading-screen"><div className="spinner"></div><span>جاري التحميل...</span></div>
        ) : departments.length === 0 ? (
          <div className="empty-state">
            <div className="empty-state-icon">🏢</div>
            <div className="empty-state-title">لا توجد أقسام</div>
          </div>
        ) : (
          <div className="table-wrapper">
            <table className="data-table">
              <thead>
                <tr>
                  <th>#</th>
                  <th>اسم القسم</th>
                  <th>الجامعة</th>
                  <th>الإجراء</th>
                </tr>
              </thead>
              <tbody>
                {departments.map((dept, idx) => (
                  <tr key={dept.id || idx}>
                    <td>{idx + 1}</td>
                    <td style={{ fontWeight: 600 }}>{dept.name}</td>
                    <td>{getUniName(dept.universityId)}</td>
                    <td>
                      <button className="btn btn-outline btn-sm" onClick={() => openEdit(dept)}>✏️ تعديل</button>
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
