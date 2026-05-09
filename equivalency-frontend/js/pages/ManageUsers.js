// Manage Users Page (Admin)
// Create, edit, and delete student and doctor accounts

function ManageUsersPage({ onNavigate }) {
  const [users, setUsers] = React.useState([]);
  const [loading, setLoading] = React.useState(true);
  const [showForm, setShowForm] = React.useState(false);
  const [editId, setEditId] = React.useState(null);
  const [formData, setFormData] = React.useState({ name: '', email: '', password: '', role: 'Student' });
  const [saving, setSaving] = React.useState(false);
  const [success, setSuccess] = React.useState('');
  const [error, setError] = React.useState('');
  const [filter, setFilter] = React.useState('الكل');
  const [deleteConfirm, setDeleteConfirm] = React.useState(null);

  React.useEffect(() => { loadData(); }, []);

  const loadData = async () => {
    try {
      const res = await adminUsersAPI.getAll();
      setUsers(Array.isArray(res.data) ? res.data : []);
    } catch (err) {
      console.error('Error loading users:', err);
    } finally {
      setLoading(false);
    }
  };

  const resetForm = () => {
    setFormData({ name: '', email: '', password: '', role: 'Student' });
    setEditId(null);
    setShowForm(false);
    setError('');
  };

  const openEdit = (u) => {
    setFormData({
      name: u.name || u.fullName || u.userName || '',
      email: u.email || '',
      password: '',
      role: u.role || 'Student',
    });
    setEditId(u.id);
    setShowForm(true);
    setError('');
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.name.trim() || !formData.email.trim()) {
      setError('يرجى إدخال الاسم والبريد الإلكتروني.');
      return;
    }
    if (!editId && !formData.password) {
      setError('يرجى إدخال كلمة المرور للحساب الجديد.');
      return;
    }
    if (formData.password && formData.password.length < 6) {
      setError('كلمة المرور يجب أن تكون 6 أحرف على الأقل.');
      return;
    }
    setSaving(true);
    setError('');
    try {
      const payload = {
        fullName: formData.name,
        email: formData.email,
        password: formData.password,
        role: formData.role,
      };
      if (editId && !formData.password) delete payload.password;

      if (editId) {
        await adminUsersAPI.update(editId, payload);
        setSuccess('تم تحديث الحساب بنجاح! ✅');
      } else {
        await adminUsersAPI.create(payload);
        setSuccess('تم إنشاء الحساب بنجاح! 🎉');
      }
      resetForm();
      loadData();
      setTimeout(() => setSuccess(''), 3000);
    } catch (err) {
      const data = err.response?.data;
      let msg = 'فشل في حفظ الحساب.';
      if (data) {
        if (typeof data === 'string') {
          msg = data;
        } else if (data.message) {
          msg = data.message;
        } else if (data.title) {
          const details = data.errors ? Object.values(data.errors).flat().join(' | ') : '';
          msg = data.title + (details ? ': ' + details : '');
        } else {
          msg = JSON.stringify(data);
        }
      }
      setError(msg);
      console.error('Create user error:', err.response?.status, data);
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (id) => {
    try {
      await adminUsersAPI.delete(id);
      setSuccess('تم حذف الحساب بنجاح. 🗑️');
      setDeleteConfirm(null);
      loadData();
      setTimeout(() => setSuccess(''), 3000);
    } catch (err) {
      setError(err.response?.data?.message || 'فشل في حذف الحساب.');
      setDeleteConfirm(null);
    }
  };

  const getRoleText = (role) => {
    if (role === 'Student') return 'طالب';
    if (role === 'Doctor') return 'دكتور';
    if (role === 'Admin') return 'مدير';
    return role || '—';
  };

  const getRoleBadge = (role) => {
    if (role === 'Student') return 'badge-pending';
    if (role === 'Doctor') return 'badge-approved';
    if (role === 'Admin') return 'badge-rejected';
    return 'badge-pending';
  };

  const filteredUsers = filter === 'الكل'
    ? users
    : users.filter(u => {
        if (filter === 'طلاب') return u.role === 'Student';
        if (filter === 'دكاترة') return u.role === 'Doctor';
        if (filter === 'مدراء') return u.role === 'Admin';
        return true;
      });

  return (
    <div className="main-content" dir="rtl">
      <div className="page-header">
        <h1 className="page-title">إدارة المستخدمين</h1>
        <p className="page-subtitle">إنشاء وتعديل وحذف حسابات الطلاب والدكاترة.</p>
      </div>

      {success && <div className="alert alert-success"><span>✅</span> {success}</div>}
      {error && !showForm && <div className="alert alert-error"><span>⚠️</span> {error}</div>}

      {/* Stats */}
      <div className="dashboard-grid" style={{ marginBottom: 24 }}>
        <div className="stat-card">
          <div className="stat-card-icon blue">👥</div>
          <div className="stat-card-value">{users.length}</div>
          <div className="stat-card-label">إجمالي المستخدمين</div>
        </div>
        <div className="stat-card">
          <div className="stat-card-icon orange">🎓</div>
          <div className="stat-card-value">{users.filter(u => u.role === 'Student').length}</div>
          <div className="stat-card-label">طلاب</div>
        </div>
        <div className="stat-card">
          <div className="stat-card-icon green">👨‍🏫</div>
          <div className="stat-card-value">{users.filter(u => u.role === 'Doctor').length}</div>
          <div className="stat-card-label">دكاترة</div>
        </div>
      </div>

      <div style={{ display: 'flex', gap: 12, marginBottom: 20, flexWrap: 'wrap', alignItems: 'center' }}>
        <button className="btn btn-primary" onClick={() => { resetForm(); setShowForm(true); }}>
          ➕ إنشاء حساب جديد
        </button>
      </div>

      {/* Filter Tabs */}
      <div className="filter-tabs" style={{ marginBottom: 16 }}>
        {['الكل', 'طلاب', 'دكاترة', 'مدراء'].map(tab => (
          <button key={tab}
            className={`btn btn-sm ${filter === tab ? 'btn-primary' : 'btn-outline'}`}
            onClick={() => setFilter(tab)}
            style={{ animationName: 'none' }}
          >
            {tab}
            <span style={{ marginRight: 6, opacity: 0.7 }}>
              ({tab === 'الكل' ? users.length :
                tab === 'طلاب' ? users.filter(u => u.role === 'Student').length :
                tab === 'دكاترة' ? users.filter(u => u.role === 'Doctor').length :
                users.filter(u => u.role === 'Admin').length})
            </span>
          </button>
        ))}
      </div>

      {/* Form */}
      {showForm && (
        <div className="form-card" style={{ marginBottom: 24 }}>
          <h2 className="card-title">{editId ? 'تعديل الحساب' : 'إنشاء حساب جديد'}</h2>
          {error && <div className="alert alert-error"><span>⚠️</span> {error}</div>}
          <form onSubmit={handleSubmit}>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
              <div className="form-group">
                <label className="form-label">الاسم الكامل *</label>
                <input type="text" className="form-input" placeholder="مثال: أحمد محمد"
                  value={formData.name} onChange={(e) => setFormData({...formData, name: e.target.value})} />
              </div>
              <div className="form-group">
                <label className="form-label">البريد الإلكتروني *</label>
                <input type="email" className="form-input" placeholder="example@yu.edu.jo" dir="ltr" style={{ textAlign: 'left' }}
                  value={formData.email} onChange={(e) => setFormData({...formData, email: e.target.value})} />
              </div>
              <div className="form-group">
                <label className="form-label">{editId ? 'كلمة المرور (اتركها فارغة لعدم التغيير)' : 'كلمة المرور *'}</label>
                <input type="password" className="form-input" placeholder="••••••••"
                  value={formData.password} onChange={(e) => setFormData({...formData, password: e.target.value})} />
              </div>
              <div className="form-group">
                <label className="form-label">الدور *</label>
                <select className="form-input" value={formData.role}
                  onChange={(e) => setFormData({...formData, role: e.target.value})}>
                  <option value="Student">طالب</option>
                  <option value="Doctor">دكتور</option>
                  <option value="Admin">مدير</option>
                </select>
              </div>
            </div>
            <div style={{ display: 'flex', gap: 10, marginTop: 16 }}>
              <button type="submit" className="btn btn-primary" disabled={saving}>
                {saving ? 'جاري الحفظ...' : editId ? '💾 تحديث' : '➕ إنشاء'}
              </button>
              <button type="button" className="btn btn-outline" onClick={resetForm}>إلغاء</button>
            </div>
          </form>
        </div>
      )}

      {/* Delete Confirmation */}
      {deleteConfirm && (
        <div className="form-card" style={{ marginBottom: 24, borderColor: '#ef4444' }}>
          <h2 className="card-title" style={{ color: '#ef4444' }}>⚠️ تأكيد الحذف</h2>
          <p style={{ marginBottom: 16 }}>هل أنت متأكد من حذف حساب <strong>{deleteConfirm.name || deleteConfirm.email}</strong>؟ لا يمكن التراجع عن هذا الإجراء.</p>
          <div style={{ display: 'flex', gap: 10 }}>
            <button className="btn btn-danger btn-sm" onClick={() => handleDelete(deleteConfirm.id)}>🗑️ نعم، احذف</button>
            <button className="btn btn-outline btn-sm" onClick={() => setDeleteConfirm(null)}>إلغاء</button>
          </div>
        </div>
      )}

      {/* Table */}
      <div className="table-container">
        <div className="table-header">
          <div className="table-title">المستخدمين ({filteredUsers.length})</div>
        </div>
        {loading ? (
          <div className="loading-screen"><div className="spinner"></div><span>جاري التحميل...</span></div>
        ) : filteredUsers.length === 0 ? (
          <div className="empty-state">
            <div className="empty-state-icon">👥</div>
            <div className="empty-state-title">لا يوجد مستخدمين</div>
          </div>
        ) : (
          <div className="table-wrapper">
            <table className="data-table">
              <thead>
                <tr>
                  <th>#</th>
                  <th>الاسم</th>
                  <th>البريد الإلكتروني</th>
                  <th>الدور</th>
                  <th>الإجراءات</th>
                </tr>
              </thead>
              <tbody>
                {filteredUsers.map((u, idx) => (
                  <tr key={u.id || idx}>
                    <td>{idx + 1}</td>
                    <td style={{ fontWeight: 600 }}>{u.name || u.fullName || u.userName || '—'}</td>
                    <td dir="ltr" style={{ textAlign: 'left' }}>{u.email || '—'}</td>
                    <td>
                      <span className={`badge ${getRoleBadge(u.role)}`}>
                        <span className="badge-dot"></span>
                        {getRoleText(u.role)}
                      </span>
                    </td>
                    <td>
                      <div style={{ display: 'flex', gap: 8 }}>
                        <button className="btn btn-outline btn-sm" onClick={() => openEdit(u)}>✏️ تعديل</button>
                        {u.role !== 'Admin' && (
                          <button className="btn btn-danger btn-sm" onClick={() => setDeleteConfirm(u)}>🗑️ حذف</button>
                        )}
                      </div>
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
