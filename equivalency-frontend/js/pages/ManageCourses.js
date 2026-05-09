// Manage Courses Page (Admin)
// Add, edit, and view courses

function ManageCoursesPage({ onNavigate }) {
  const [courses, setCourses] = React.useState([]);
  const [universities, setUniversities] = React.useState([]);
  const [departments, setDepartments] = React.useState([]);
  const [loading, setLoading] = React.useState(true);
  const [showForm, setShowForm] = React.useState(false);
  const [editId, setEditId] = React.useState(null);
  const [formData, setFormData] = React.useState({ courseName: '', courseCode: '', creditHours: '', universityId: '', departmentId: '', description: '' });
  const [saving, setSaving] = React.useState(false);
  const [success, setSuccess] = React.useState('');
  const [error, setError] = React.useState('');

  React.useEffect(() => { loadData(); }, []);

  const loadData = async () => {
    try {
      const [cRes, uRes, dRes] = await Promise.all([
        adminCoursesAPI.getAll(),
        adminUniversitiesAPI.getAll().catch(() => ({ data: [] })),
        adminDepartmentsAPI.getAll().catch(() => ({ data: [] })),
      ]);
      setCourses(Array.isArray(cRes.data) ? cRes.data : []);
      setUniversities(Array.isArray(uRes.data) ? uRes.data : []);
      setDepartments(Array.isArray(dRes.data) ? dRes.data : []);
    } catch (err) {
      console.error('Error loading courses:', err);
    } finally {
      setLoading(false);
    }
  };

  const resetForm = () => {
    setFormData({ courseName: '', courseCode: '', creditHours: '', universityId: '', departmentId: '', description: '' });
    setEditId(null);
    setShowForm(false);
    setError('');
  };

  const openEdit = (c) => {
    setFormData({
      courseName: c.courseName || c.name || '',
      courseCode: c.courseCode || c.code || '',
      creditHours: c.creditHours || '',
      universityId: c.universityId || '',
      departmentId: c.departmentId || '',
      description: c.description || '',
    });
    setEditId(c.id);
    setShowForm(true);
    setError('');
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.courseName.trim()) { setError('يرجى إدخال اسم المساق.'); return; }
    setSaving(true);
    setError('');
    try {
      const payload = { ...formData, creditHours: Number(formData.creditHours) || 0 };
      if (editId) {
        await adminCoursesAPI.update(editId, payload);
        setSuccess('تم تحديث المساق بنجاح! ✅');
      } else {
        await adminCoursesAPI.create(payload);
        setSuccess('تم إضافة المساق بنجاح! 🎉');
      }
      resetForm();
      loadData();
      setTimeout(() => setSuccess(''), 3000);
    } catch (err) {
      setError(err.response?.data?.message || 'فشل في حفظ المساق.');
    } finally {
      setSaving(false);
    }
  };

  const getUniName = (id) => { const u = universities.find(u => u.id === id); return u ? u.name : '—'; };
  const getDeptName = (id) => { const d = departments.find(d => d.id === id); return d ? d.name : '—'; };

  return (
    <div className="main-content" dir="rtl">
      <div className="page-header">
        <h1 className="page-title">إدارة المساقات</h1>
        <p className="page-subtitle">إضافة وتعديل المساقات الجامعية المتاحة للمعادلة.</p>
      </div>

      {success && <div className="alert alert-success"><span>✅</span> {success}</div>}

      <div style={{ marginBottom: 20 }}>
        <button className="btn btn-primary" onClick={() => { resetForm(); setShowForm(true); }}>
          ➕ إضافة مساق جديد
        </button>
      </div>

      {showForm && (
        <div className="form-card" style={{ marginBottom: 24 }}>
          <h2 className="card-title">{editId ? 'تعديل المساق' : 'إضافة مساق جديد'}</h2>
          {error && <div className="alert alert-error"><span>⚠️</span> {error}</div>}
          <form onSubmit={handleSubmit}>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
              <div className="form-group">
                <label className="form-label">اسم المساق *</label>
                <input type="text" className="form-input" placeholder="مثال: أمن معلومات"
                  value={formData.courseName} onChange={(e) => setFormData({...formData, courseName: e.target.value})} />
              </div>
              <div className="form-group">
                <label className="form-label">رمز المساق</label>
                <input type="text" className="form-input" placeholder="مثال: CS401"
                  value={formData.courseCode} onChange={(e) => setFormData({...formData, courseCode: e.target.value})} />
              </div>
              <div className="form-group">
                <label className="form-label">الساعات المعتمدة</label>
                <input type="number" className="form-input" placeholder="3" min="1"
                  value={formData.creditHours} onChange={(e) => setFormData({...formData, creditHours: e.target.value})} />
              </div>
              <div className="form-group">
                <label className="form-label">الجامعة</label>
                <select className="form-input" value={formData.universityId}
                  onChange={(e) => setFormData({...formData, universityId: e.target.value})}>
                  <option value="">— اختر الجامعة —</option>
                  {universities.map(u => <option key={u.id} value={u.id}>{u.name}</option>)}
                </select>
              </div>
              <div className="form-group">
                <label className="form-label">القسم</label>
                <select className="form-input" value={formData.departmentId}
                  onChange={(e) => setFormData({...formData, departmentId: e.target.value})}>
                  <option value="">— اختر القسم —</option>
                  {departments.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
                </select>
              </div>
            </div>
            <div className="form-group">
              <label className="form-label">الوصف</label>
              <textarea className="form-input" rows="3" placeholder="وصف مختصر للمساق..."
                value={formData.description} onChange={(e) => setFormData({...formData, description: e.target.value})} />
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
          <div className="table-title">المساقات ({courses.length})</div>
        </div>
        {loading ? (
          <div className="loading-screen"><div className="spinner"></div><span>جاري التحميل...</span></div>
        ) : courses.length === 0 ? (
          <div className="empty-state">
            <div className="empty-state-icon">📚</div>
            <div className="empty-state-title">لا توجد مساقات</div>
          </div>
        ) : (
          <div className="table-wrapper">
            <table className="data-table">
              <thead>
                <tr>
                  <th>#</th>
                  <th>اسم المساق</th>
                  <th>الرمز</th>
                  <th>الساعات</th>
                  <th>الجامعة</th>
                  <th>القسم</th>
                  <th>الإجراء</th>
                </tr>
              </thead>
              <tbody>
                {courses.map((c, idx) => (
                  <tr key={c.id || idx}>
                    <td>{idx + 1}</td>
                    <td style={{ fontWeight: 600 }}>{c.courseName || c.name || '—'}</td>
                    <td>{c.courseCode || c.code || '—'}</td>
                    <td>{c.creditHours || '—'}</td>
                    <td>{getUniName(c.universityId)}</td>
                    <td>{getDeptName(c.departmentId)}</td>
                    <td>
                      <button className="btn btn-outline btn-sm" onClick={() => openEdit(c)}>✏️ تعديل</button>
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
