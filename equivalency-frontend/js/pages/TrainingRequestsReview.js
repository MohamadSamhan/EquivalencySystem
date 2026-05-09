// Training Requests Review Page
// Separate page for training certificate requests (Doctor/Admin)

function TrainingRequestsReviewBase({ mode }) {
  const isDoctorMode = mode === 'doctor';
  const [requests, setRequests] = React.useState([]);
  const [loading, setLoading] = React.useState(true);
  const [actionLoading, setActionLoading] = React.useState(null);
  const [filter, setFilter] = React.useState('قيد الانتظار');
  const [successMsg, setSuccessMsg] = React.useState('');

  React.useEffect(() => {
    loadRequests();
  }, []);

  const loadRequests = async () => {
    setLoading(true);
    try {
      const res = await trainingAPI.getAll().catch(() => ({ data: [] }));
      setRequests(Array.isArray(res.data) ? res.data : []);
    } catch (err) {
      console.error('Error loading training requests:', err);
    } finally {
      setLoading(false);
    }
  };

  const getStatusString = (statusCode) => {
    if (statusCode === 0 || statusCode === 'Pending') return 'Pending';
    if (statusCode === 1 || statusCode === 'Approved') return 'Approved';
    if (statusCode === 2 || statusCode === 'Rejected') return 'Rejected';
    return String(statusCode);
  };

  const getBadgeClass = (status) => {
    const s = getStatusString(status);
    switch (s) {
      case 'Approved': return 'badge-approved';
      case 'Rejected': return 'badge-rejected';
      default: return 'badge-pending';
    }
  };

  const getStatusText = (status) => {
    const s = getStatusString(status);
    switch (s) {
      case 'Approved': return 'تمت الموافقة';
      case 'Rejected': return 'تم الرفض';
      default: return 'قيد الانتظار';
    }
  };

  const filteredRequests = filter === 'الكل'
    ? requests
    : requests.filter((r) => {
        const s = getStatusString(r.status);
        if (filter === 'قيد الانتظار') return s === 'Pending';
        if (filter === 'تمت الموافقة') return s === 'Approved';
        if (filter === 'تم الرفض') return s === 'Rejected';
        return true;
      });

  const handleDownloadFile = async (fileUrl) => {
    if (!fileUrl) return;
    try {
      const token = localStorage.getItem('token');
      const fullUrl = fileUrl.startsWith('http') ? fileUrl : `https://localhost:7012${fileUrl}`;
      const res = await fetch(fullUrl, {
        headers: { Authorization: `Bearer ${token}` }
      });
      if (!res.ok) {
        alert('تعذر تحميل ملف الشهادة.');
        return;
      }

      const blob = await res.blob();
      const contentDisposition = res.headers.get('content-disposition') || '';
      const nameMatch = contentDisposition.match(/filename\*?=(?:UTF-8'')?([^;\n"]+)/i);
      const fileName = nameMatch ? decodeURIComponent(nameMatch[1].replace(/"/g, '')) : 'training_certificate.pdf';
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = fileName;
      a.target = '_blank';
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    } catch (err) {
      alert('حدث خطا اثناء تحميل الملف.');
    }
  };

  const handleApprove = async (id) => {
    if (!isDoctorMode) return;
    setActionLoading(id);
    try {
      await trainingAPI.approve(id);
      setRequests(requests.map((r) => r.id === id ? { ...r, status: 'Approved' } : r));
      setSuccessMsg('تمت الموافقة على طلب الشهادة التدريبية.');
      setTimeout(() => setSuccessMsg(''), 3000);
    } catch (err) {
      console.error('Approve training request error:', err);
      alert('فشل في الموافقة على الطلب.');
    } finally {
      setActionLoading(null);
    }
  };

  const handleReject = async (id) => {
    if (!isDoctorMode) return;
    if (!confirm('هل انت متاكد انك تريد رفض هذا الطلب؟')) return;

    setActionLoading(id);
    try {
      await trainingAPI.reject(id);
      setRequests(requests.map((r) => r.id === id ? { ...r, status: 'Rejected' } : r));
      setSuccessMsg('تم رفض طلب الشهادة التدريبية.');
      setTimeout(() => setSuccessMsg(''), 3000);
    } catch (err) {
      console.error('Reject training request error:', err);
      alert('فشل في رفض الطلب.');
    } finally {
      setActionLoading(null);
    }
  };

  const pageTitle = isDoctorMode ? 'مراجعة طلبات الشهادات التدريبية' : 'متابعة طلبات الشهادات التدريبية';
  const pageSubtitle = isDoctorMode
    ? 'قسم مخصص لمراجعة طلبات معادلة الشهادات التدريبية واتخاذ القرار.'
    : 'قسم مخصص لمتابعة طلبات معادلة الشهادات التدريبية (عرض فقط).';

  return (
    <div className="main-content" dir="rtl">
      <div className="page-header">
        <h1 className="page-title">{pageTitle}</h1>
        <p className="page-subtitle">{pageSubtitle}</p>
      </div>

      {successMsg && (
        <div className="alert alert-success">
          <span>✅</span> {successMsg}
        </div>
      )}

      <div className="dashboard-grid" style={{ marginBottom: 24 }}>
        <div className="stat-card">
          <div className="stat-card-icon blue">🎓</div>
          <div className="stat-card-value">{requests.length}</div>
          <div className="stat-card-label">اجمالي الطلبات</div>
        </div>
        <div className="stat-card">
          <div className="stat-card-icon orange">⏳</div>
          <div className="stat-card-value">{requests.filter((r) => getStatusString(r.status) === 'Pending').length}</div>
          <div className="stat-card-label">قيد الانتظار</div>
        </div>
        <div className="stat-card">
          <div className="stat-card-icon green">✅</div>
          <div className="stat-card-value">{requests.filter((r) => getStatusString(r.status) === 'Approved').length}</div>
          <div className="stat-card-label">تمت الموافقة</div>
        </div>
        <div className="stat-card">
          <div className="stat-card-icon red">❌</div>
          <div className="stat-card-value">{requests.filter((r) => getStatusString(r.status) === 'Rejected').length}</div>
          <div className="stat-card-label">تم الرفض</div>
        </div>
      </div>

      <div className="filter-tabs">
        {['قيد الانتظار', 'تمت الموافقة', 'تم الرفض', 'الكل'].map((tab) => (
          <button
            key={tab}
            className={`btn btn-sm ${filter === tab ? 'btn-primary' : 'btn-outline'}`}
            onClick={() => setFilter(tab)}
            style={{ animationName: 'none' }}
          >
            {tab}
            <span style={{ marginRight: 6, opacity: 0.7 }}>
              ({tab === 'الكل' ? requests.length : requests.filter((r) => {
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
          <div className="table-title">طلبات {filter} ({filteredRequests.length})</div>
          <button className="btn btn-outline btn-sm" onClick={loadRequests}>تحديث</button>
        </div>

        {loading ? (
          <div className="loading-screen">
            <div className="spinner"></div>
            <span>جاري تحميل الطلبات...</span>
          </div>
        ) : filteredRequests.length === 0 ? (
          <div className="empty-state">
            <div className="empty-state-icon">🎓</div>
            <div className="empty-state-title">لا توجد طلبات {filter}</div>
            <div className="empty-state-desc">لا توجد طلبات شهادات تدريبية في الوقت الحالي.</div>
          </div>
        ) : (
          <div className="table-wrapper">
            <table className="data-table">
              <thead>
                <tr>
                  <th>#</th>
                  <th>الطالب</th>
                  <th>عنوان التدريب</th>
                  <th>الجهة المقدمة</th>
                  <th>الساعات</th>
                  <th>ملف الشهادة</th>
                  <th>الحالة</th>
                  <th>الإجراء</th>
                </tr>
              </thead>
              <tbody>
                {filteredRequests.map((req, idx) => {
                  const studentName = req.studentName || req.student?.name || req.student?.userName || `طالب #${req.studentId || ''}`;
                  const title = req.trainingTitle || req.title || 'غير متوفر';
                  const provider = req.trainingProvider || req.providerName || 'غير متوفر';
                  const hours = req.trainingHours ?? req.hours ?? '—';
                  const certFileUrl = req.certificateFileUrl || req.certificateUrl || req.fileUrl || req.documentUrl || null;
                  const rowId = req.id || idx;

                  return (
                    <tr key={rowId}>
                      <td>{idx + 1}</td>
                      <td style={{ fontWeight: 600 }}>{studentName}</td>
                      <td>{title}</td>
                      <td>{provider}</td>
                      <td>{hours}</td>
                      <td>
                        {certFileUrl ? (
                          <button
                            className="btn btn-outline btn-sm"
                            onClick={() => handleDownloadFile(certFileUrl)}
                            style={{ gap: 4 }}
                          >
                            📄 عرض الشهادة
                          </button>
                        ) : (
                          <span style={{ color: '#64748b', fontSize: 12 }}>لا يوجد</span>
                        )}
                      </td>
                      <td>
                        <span className={`badge ${getBadgeClass(req.status)}`}>
                          <span className="badge-dot"></span>
                          {getStatusText(req.status)}
                        </span>
                      </td>
                      <td>
                        {isDoctorMode ? (
                          getStatusString(req.status) === 'Pending' ? (
                            <div style={{ display: 'flex', gap: 8 }}>
                              <button
                                className="btn btn-success btn-sm"
                                onClick={() => handleApprove(req.id)}
                                disabled={actionLoading === req.id}
                              >
                                {actionLoading === req.id ? '...' : '✅ موافقة'}
                              </button>
                              <button
                                className="btn btn-danger btn-sm"
                                onClick={() => handleReject(req.id)}
                                disabled={actionLoading === req.id}
                              >
                                {actionLoading === req.id ? '...' : '❌ رفض'}
                              </button>
                            </div>
                          ) : (
                            <span style={{ color: '#64748b', fontSize: 13 }}>{getStatusText(req.status)}</span>
                          )
                        ) : (
                          <span style={{ color: '#64748b', fontSize: 13 }}>عرض فقط</span>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}

function TrainingRequestsReviewPage() {
  return <TrainingRequestsReviewBase mode="doctor" />;
}

function AdminTrainingRequestsPage() {
  return <TrainingRequestsReviewBase mode="admin" />;
}
