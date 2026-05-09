// Request Equivalency Page (Student)
// Supports internal and external transfer scenarios with dynamic fields

function RequestsPage({ onNavigate }) {
  const [transferType, setTransferType] = React.useState('');
  const [sourceType, setSourceType] = React.useState('');
  const [transcriptFile, setTranscriptFile] = React.useState(null);
  const [extractedCourses, setExtractedCourses] = React.useState([]);

  const [jordanianUniversities, setJordanianUniversities] = React.useState([]);

  const [selectedUniversity, setSelectedUniversity] = React.useState('');
  const [oldStudentId, setOldStudentId] = React.useState('');

  const [nonJordanianUniversityName, setNonJordanianUniversityName] = React.useState('');
  const [nonJordanianMajorName, setNonJordanianMajorName] = React.useState('');

  const [loadingMeta, setLoadingMeta] = React.useState(true);
  const [extracting, setExtracting] = React.useState(false);
  const [submitting, setSubmitting] = React.useState(false);
  const [success, setSuccess] = React.useState('');
  const [error, setError] = React.useState('');
  const [info, setInfo] = React.useState('');
    const [submissionResults, setSubmissionResults] = React.useState([]);
    const resultsRef = React.useRef(null);

  const fallbackUniversities = [
    { id: 1, name: 'الجامعة الأردنية' },
    { id: 2, name: 'جامعة اليرموك' },
    { id: 3, name: 'جامعة العلوم والتكنولوجيا الأردنية' },
    { id: 4, name: 'الجامعة الهاشمية' },
  ];

  React.useEffect(() => {
    loadUniversities();
  }, []);

  React.useEffect(() => {
    if (transferType !== 'External Transfer') {
      setSourceType('');
      setSelectedUniversity('');
      setOldStudentId('');
      setNonJordanianUniversityName('');
      setNonJordanianMajorName('');
    }
  }, [transferType]);

  const loadUniversities = async () => {
    try {
      const res = await universitiesAPI.getAll();
      const universities = Array.isArray(res.data)
        ? res.data
        : Array.isArray(res.data?.items)
          ? res.data.items
          : [];

      if (universities.length > 0) {
        setJordanianUniversities(universities.map((u, idx) => ({ id: u.id ?? idx + 1, name: u.name || u.universityName || `جامعة ${idx + 1}` })));
      } else {
        setJordanianUniversities(fallbackUniversities);
      }
    } catch (err) {
      setJordanianUniversities(fallbackUniversities);
    } finally {
      setLoadingMeta(false);
    }
  };


  const transferTypeMap = {
    'Internal Transfer': 0,
    'External Transfer': 1,
  };

  const sourceTypeMap = {
    '': 0,
    'Jordanian University': 1,
    'Non-Jordanian University': 2,
  };

  const extractCoursesFromTranscript = async (file) => {
    setExtracting(true);
    setError('');
    setInfo('');
    try {
      const formData = new FormData();
      formData.append('transferType', transferTypeMap[transferType] ?? 0);
      formData.append('sourceType', sourceTypeMap[sourceType] ?? 0);
      formData.append('transcriptFile', file);

      const res = await api.post('/equivalency/extract-courses', formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      });

      console.log('extract-courses response:', JSON.stringify(res.data));

      const rawCourses = Array.isArray(res.data)
        ? res.data
        : Array.isArray(res.data?.results)
          ? res.data.results
          : Array.isArray(res.data?.courses)
            ? res.data.courses
            : Array.isArray(res.data?.extractedCourses)
              ? res.data.extractedCourses
              : Array.isArray(res.data?.data)
                ? res.data.data
                : Array.isArray(res.data?.items)
                  ? res.data.items
                  : [];

      const normalized = rawCourses.map((course, idx) => ({
        id: course.id ?? course.equivalencyRequestId ?? idx + 1,
        equivalencyRequestId: course.equivalencyRequestId ?? null,
        courseName: course.courseName || course.name || course.title || 'Course',
        grade: course.grade || course.letterGrade || '-',
        creditHours: course.creditHours ?? course.credits ?? '-',
        courseCode: course.courseCode || '',
        matchedCourseId: course.matchedCourseId ?? null,
        matchedCourseName: course.matchedCourseName || '',
        similarityScore: course.similarityScore ?? course.similarity ?? course.similarityPercentage ?? null,
        decision: course.decision || 'NotEquivalent',
      }));

      if (normalized.length === 0) {
        setError('تم رفع الملف لكن لم يتم العثور على مساقات داخله.');
      }

      setExtractedCourses(normalized);
    } catch (err) {
      setExtractedCourses([
        { id: 1, courseName: 'Data Structures', grade: 'A-', creditHours: 3 },
        { id: 2, courseName: 'Database Systems', grade: 'B+', creditHours: 3 },
        { id: 3, courseName: 'Operating Systems', grade: 'B', creditHours: 3 },
      ]);
      setInfo('خدمة الاستخراج غير متاحة حالياً، تم عرض بيانات تجريبية.');
    } finally {
      setExtracting(false);
    }
  };

  const handleTranscriptFile = (e) => {
    const file = e.target.files[0];
    setError('');
    setSuccess('');
    setInfo('');

    if (!file) {
      setTranscriptFile(null);
      setExtractedCourses([]);
      return;
    }

    if (file.type !== 'application/pdf') {
      setError('يرجى رفع ملف PDF فقط.');
      setTranscriptFile(null);
      setExtractedCourses([]);
      return;
    }

    if (!transferType) {
      setError('يرجى اختيار نوع التحويل أولاً.');
      setTranscriptFile(null);
      setExtractedCourses([]);
      return;
    }

    if (transferType === 'External Transfer' && !sourceType) {
      setError('يرجى اختيار نوع الجامعة المحوّل منها أولاً.');
      setTranscriptFile(null);
      setExtractedCourses([]);
      return;
    }

    setTranscriptFile(file);
    setExtractedCourses([]);
    extractCoursesFromTranscript(file);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setSuccess('');
    setInfo('');

    if (!transferType) {
      setError('يرجى اختيار نوع التحويل.');
      return;
    }

    if (transferType === 'Internal Transfer' && !oldStudentId.trim()) {
      setError('يرجى إدخال رقمك الجامعي القديم للتحويل الداخلي.');
      return;
    }

    if (!transcriptFile) {
      setError('يرجى رفع كشف العلامات بصيغة PDF.');
      return;
    }

    if (extractedCourses.length === 0) {
      setError('يرجى استخراج المساقات من كشف العلامات قبل التقديم.');
      return;
    }

    if (transferType === 'External Transfer') {
      if (!sourceType) {
        setError('يرجى اختيار نوع الجامعة المحوّل منها.');
        return;
      }

      if (sourceType === 'Jordanian University') {
        if (!selectedUniversity || !oldStudentId.trim()) {
          setError('يرجى اختيار الجامعة الأردنية وإدخال الرقم الجامعي القديم.');
          return;
        }
      }

      if (sourceType === 'Non-Jordanian University') {
        if (!nonJordanianUniversityName.trim() || !nonJordanianMajorName.trim() || !oldStudentId.trim()) {
          setError('يرجى تعبئة بيانات الجامعة غير الأردنية كاملة.');
          return;
        }
      }
    }

    setSubmitting(true);
    try {
      // Always show comparison output from extracted courses on submit click.
      setSubmissionResults(extractedCourses);

      // Some backends persist equivalency rows during extraction and return equivalencyRequestId.
      // In that case, avoid submitting again to prevent duplicate-processing errors.
      const alreadyPersistedFromExtraction = extractedCourses.length > 0
        && extractedCourses.every(c => c.equivalencyRequestId != null || c.id === c.equivalencyRequestId);
      if (alreadyPersistedFromExtraction) {
        setSuccess('تم حفظ الطلب وعرض نتائج المقارنة بنجاح! ✅');
        setTimeout(() => {
          if (resultsRef.current) {
            resultsRef.current.scrollIntoView({ behavior: 'smooth', block: 'start' });
          }
        }, 200);
        return;
      }

      const formData = new FormData();
      formData.append('transferType', transferType);
      formData.append('sourceType', sourceType || '');
      formData.append('oldStudentId', oldStudentId || '');
      formData.append('sourceUniversityId', selectedUniversity || '');
      formData.append('sourceUniversityName', nonJordanianUniversityName || '');
      formData.append('sourceMajorName', nonJordanianMajorName || '');
      formData.append('transcriptFile', transcriptFile);
      formData.append('extractedCoursesJson', JSON.stringify(extractedCourses));

        const res = await api.post('/equivalency-requests', formData);

        const rawResults = Array.isArray(res.data) ? res.data
          : Array.isArray(res.data?.results) ? res.data.results
          : Array.isArray(res.data?.courses) ? res.data.courses
          : [];

        if (rawResults.length > 0) {
          setSubmissionResults(rawResults);
        }

        setSuccess('تم تقديم طلب معادلة المساقات بنجاح! ✅');
        setTransferType('');
        setSourceType('');
        setTranscriptFile(null);
        setExtractedCourses([]);
        setSelectedUniversity('');
        setOldStudentId('');
        setNonJordanianUniversityName('');
        setNonJordanianMajorName('');
        const fileInput = document.getElementById('transcript-upload');
        if (fileInput) fileInput.value = '';

        setTimeout(() => {
          if (resultsRef.current) {
            resultsRef.current.scrollIntoView({ behavior: 'smooth', block: 'start' });
          }
        }, 200);
    } catch (err) {
        const apiMessage = err.response?.data?.message || err.response?.data || '';
        const isStudentCourseLinkIssue = String(apiMessage).includes('Student course not found or does not belong to student');

        // Keep comparison visible even when save fails in backend.
        setTimeout(() => {
          if (resultsRef.current) {
            resultsRef.current.scrollIntoView({ behavior: 'smooth', block: 'start' });
          }
        }, 200);

        if (isStudentCourseLinkIssue) {
          setInfo('تمت المقارنة وعرض النتائج، لكن لم يتم حفظ الطلب في النظام بسبب عدم تطابق studentCourseId في الباكند.');
        } else {
          setError(err.response?.data?.message || 'تم عرض نتائج المقارنة، لكن فشل حفظ الطلب. يرجى المحاولة مرة أخرى.');
        }
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="main-content" dir="rtl">
      <div className="page-header">
        <h1 className="page-title">طلب معادلة المساقات</h1>
        <p className="page-subtitle">اتبع الخطوات التالية لتقديم طلب معادلة بناءً على كشف العلامات.</p>
      </div>

      {success && (
        <div className="alert alert-success">
          <span>✅</span> {success}
        </div>
      )}

      {info && (
        <div className="alert alert-info">
          <span>ℹ️</span> {info}
        </div>
      )}

      {error && (
        <div className="alert alert-error">
          <span>⚠️</span> {error}
        </div>
      )}

      <div className="request-layout">
        <div className="form-card" style={{ maxWidth: '100%' }}>
          <h2 className="card-title">نموذج طلب المعادلة</h2>

          {loadingMeta ? (
            <div className="loading-screen" style={{ minHeight: 200 }}>
              <div className="spinner"></div>
              <span>جاري تحميل بيانات الكليات والأقسام...</span>
            </div>
          ) : (
            <form onSubmit={handleSubmit}>
              <div className="form-group">
                <label className="form-label" htmlFor="transfer-type-select">
                  الخطوة 1: نوع التحويل <span style={{ color: 'var(--danger-500)' }}>*</span>
                </label>
                <select
                  id="transfer-type-select"
                  value={transferType}
                  onChange={(e) => setTransferType(e.target.value)}
                >
                  <option value="">اختر نوع التحويل</option>
                  <option value="Internal Transfer">تحويل داخلي</option>
                  <option value="External Transfer">تحويل خارجي</option>
                </select>
              </div>

              {transferType === 'Internal Transfer' && (
                <div className="form-group">
                  <label className="form-label" htmlFor="old-student-id-internal">
                    الرقم الجامعي القديم <span style={{ color: 'var(--danger-500)' }}>*</span>
                  </label>
                  <input
                    id="old-student-id-internal"
                    type="text"
                    value={oldStudentId}
                    onChange={(e) => setOldStudentId(e.target.value)}
                    placeholder="أدخل رقمك الجامعي القديم"
                  />
                </div>
              )}

              {transferType === 'External Transfer' && (
                <div className="form-group">
                  <label className="form-label" htmlFor="source-type-select">
                    الخطوة 2: نوع الجهة المحوّل منها <span style={{ color: 'var(--danger-500)' }}>*</span>
                  </label>
                  <select
                    id="source-type-select"
                    value={sourceType}
                    onChange={(e) => setSourceType(e.target.value)}
                  >
                    <option value="">اختر نوع الجامعة المحوّل منها</option>
                    <option value="Jordanian University">جامعة أردنية</option>
                    <option value="Non-Jordanian University">جامعة غير أردنية</option>
                  </select>
                </div>
              )}

              {transferType === 'External Transfer' && sourceType === 'Jordanian University' && (
                <div className="form-row">
                  <div className="form-group">
                    <label className="form-label" htmlFor="university-select">
                      الجامعة الأردنية <span style={{ color: 'var(--danger-500)' }}>*</span>
                    </label>
                    <select
                      id="university-select"
                      value={selectedUniversity}
                      onChange={(e) => setSelectedUniversity(e.target.value)}
                    >
                      <option value="">اختر الجامعة</option>
                      {jordanianUniversities.map((u) => (
                        <option key={u.id} value={u.id}>{u.name}</option>
                      ))}
                    </select>
                  </div>

                  <div className="form-group">
                    <label className="form-label" htmlFor="old-student-id-jordanian">
                      الرقم الجامعي القديم <span style={{ color: 'var(--danger-500)' }}>*</span>
                    </label>
                    <input
                      id="old-student-id-jordanian"
                      type="text"
                      value={oldStudentId}
                      onChange={(e) => setOldStudentId(e.target.value)}
                      placeholder="أدخل رقمك الجامعي القديم"
                    />
                  </div>
                </div>
              )}

              {transferType === 'External Transfer' && sourceType === 'Non-Jordanian University' && (
                <div className="form-row">
                  <div className="form-group">
                    <label className="form-label" htmlFor="non-jordanian-university-name">
                      اسم الجامعة <span style={{ color: 'var(--danger-500)' }}>*</span>
                    </label>
                    <input
                      id="non-jordanian-university-name"
                      type="text"
                      value={nonJordanianUniversityName}
                      onChange={(e) => setNonJordanianUniversityName(e.target.value)}
                      placeholder="أدخل اسم الجامعة"
                    />
                  </div>

                  <div className="form-group">
                    <label className="form-label" htmlFor="non-jordanian-major-name">
                      اسم التخصص <span style={{ color: 'var(--danger-500)' }}>*</span>
                    </label>
                    <input
                      id="non-jordanian-major-name"
                      type="text"
                      value={nonJordanianMajorName}
                      onChange={(e) => setNonJordanianMajorName(e.target.value)}
                      placeholder="أدخل اسم التخصص"
                    />
                  </div>

                  <div className="form-group">
                    <label className="form-label" htmlFor="old-student-id-non-jordanian">
                      الرقم الجامعي القديم <span style={{ color: 'var(--danger-500)' }}>*</span>
                    </label>
                    <input
                      id="old-student-id-non-jordanian"
                      type="text"
                      value={oldStudentId}
                      onChange={(e) => setOldStudentId(e.target.value)}
                      placeholder="أدخل رقمك الجامعي القديم"
                    />
                  </div>
                </div>
              )}

              <div className="form-group">
                <label className="form-label" htmlFor="transcript-upload">
                  رفع كشف العلامات (PDF) <span style={{ color: 'var(--danger-500)' }}>*</span>
                </label>
                <div
                  className={`file-upload-zone${transcriptFile ? ' has-file' : ''}`}
                  onClick={() => document.getElementById('transcript-upload').click()}
                >
                  <input
                    id="transcript-upload"
                    type="file"
                    accept=".pdf"
                    style={{ display: 'none' }}
                    onChange={handleTranscriptFile}
                  />
                  <span className="file-upload-icon">{transcriptFile ? '✅' : '📄'}</span>
                  <div className="file-upload-text">
                    {transcriptFile ? `تم اختيار الملف: ${transcriptFile.name}` : 'اضغط هنا لرفع ملف كشف العلامات (PDF)'}
                  </div>
                </div>
                <div className="form-actions" style={{ marginTop: 12 }}>
                  {extracting && <span style={{ color: 'var(--text-muted)', fontSize: 13 }}>جاري استخراج المساقات من كشف العلامات...</span>}
                </div>
              </div>

              <div className="form-group">
                <label className="form-label">
                  الخطوة 4: المساقات المستخرجة
                </label>
                {extractedCourses.length === 0 ? (
                  <div className="empty-state" style={{ padding: '26px 14px', border: '1px dashed var(--border-default)', borderRadius: 12 }}>
                    <div className="empty-state-icon" style={{ fontSize: 28, marginBottom: 8 }}>📚</div>
                    <div className="empty-state-title" style={{ fontSize: 14 }}>لم يتم استخراج مساقات بعد</div>
                  </div>
                ) : (
                  <div className="table-container" style={{ borderRadius: 12 }}>
                    <div className="table-wrapper">
                      <table className="data-table">
                        <thead>
                          <tr>
                            <th>#</th>
                            <th>اسم المساق</th>
                            <th>العلامة</th>
                            <th>الساعات المعتمدة</th>
                          </tr>
                        </thead>
                        <tbody>
                          {extractedCourses.map((course, idx) => (
                            <tr key={course.id || idx}>
                              <td>{idx + 1}</td>
                              <td style={{ fontWeight: 600 }}>{course.courseName}</td>
                              <td>{course.grade}</td>
                              <td>{course.creditHours}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  </div>
                )}
              </div>

              <div className="form-actions">
                <button
                  type="submit"
                  className="btn btn-primary"
                  disabled={submitting}
                  id="submit-request-btn"
                >
                  {submitting ? (
                    <>
                      <span className="spinner"></span>
                      جاري إرسال الطلب...
                    </>
                  ) : (
                    <>الخطوة 5: إرسال الطلب</>
                  )}
                </button>

                <button
                  type="button"
                  className="btn btn-outline"
                  onClick={() => onNavigate('results')}
                >
                  عرض النتائج
                </button>
              </div>
            </form>
          )}
        </div>

        <div className="how-it-works-card">
          <h3>تسلسل الخطوات</h3>
          {[
            'الخطوة 1: اختر نوع التحويل (داخلي أو خارجي).',
            'الخطوة 2: في التحويل الداخلي أدخل رقمك الجامعي القديم.',
            'الخطوة 3: في التحويل الخارجي اختر نوع الجهة ثم عبّئ البيانات المطلوبة.',
            'الخطوة 4: ارفع كشف العلامات وسيتم استخراج المساقات تلقائياً.',
            'الخطوة 5: أرسل الطلب إلى النظام.'
          ].map((step, idx) => (
            <div className="step-item" key={idx}>
              <div className="step-number">{idx + 1}</div>
              <div className="step-content"><strong>{step}</strong></div>
            </div>
          ))}
        </div>
      </div>

        {submissionResults.length > 0 && (
          <div ref={resultsRef} className="form-card" style={{ marginTop: 32, maxWidth: '100%' }}>
            <h2 className="card-title" style={{ marginBottom: 8 }}>نتائج مقارنة المساقات</h2>
            <p style={{ color: 'var(--text-muted)', marginBottom: 20, fontSize: 14 }}>
              نتائج مطابقة مساقاتك مع مساقات قاعدة البيانات بناءً على الذكاء الاصطناعي.
            </p>

            {(() => {
              const equivalent = submissionResults.filter(r => r.decision === 'Equivalent');
              const notEquivalent = submissionResults.filter(r => r.decision !== 'Equivalent');
              return (
                <div style={{ display: 'flex', gap: 16, marginBottom: 20, flexWrap: 'wrap' }}>
                  <div style={{ background: 'var(--success-500, #22c55e)22', border: '1px solid var(--success-500, #22c55e)', borderRadius: 10, padding: '10px 20px', textAlign: 'center' }}>
                    <div style={{ fontSize: 22, fontWeight: 700, color: 'var(--success-500, #22c55e)' }}>{equivalent.length}</div>
                    <div style={{ fontSize: 13, color: 'var(--text-muted)' }}>مساق معادَل</div>
                  </div>
                  <div style={{ background: 'var(--danger-500, #ef4444)22', border: '1px solid var(--danger-500, #ef4444)', borderRadius: 10, padding: '10px 20px', textAlign: 'center' }}>
                    <div style={{ fontSize: 22, fontWeight: 700, color: 'var(--danger-500, #ef4444)' }}>{notEquivalent.length}</div>
                    <div style={{ fontSize: 13, color: 'var(--text-muted)' }}>مساق غير معادَل</div>
                  </div>
                  <div style={{ background: 'var(--primary-500, #6366f1)22', border: '1px solid var(--primary-500, #6366f1)', borderRadius: 10, padding: '10px 20px', textAlign: 'center' }}>
                    <div style={{ fontSize: 22, fontWeight: 700, color: 'var(--primary-500, #6366f1)' }}>{submissionResults.length}</div>
                    <div style={{ fontSize: 13, color: 'var(--text-muted)' }}>إجمالي المساقات</div>
                  </div>
                </div>
              );
            })()}

            <div className="table-container" style={{ borderRadius: 12 }}>
              <div className="table-wrapper">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>#</th>
                      <th>اسم المساق (القديم)</th>
                      <th>العلامة</th>
                      <th>الساعات</th>
                      <th>المساق المطابق</th>
                      <th>نسبة التشابه</th>
                      <th>القرار</th>
                    </tr>
                  </thead>
                  <tbody>
                    {submissionResults.map((r, idx) => {
                      const isEquivalent = r.decision === 'Equivalent';
                      const similarity = r.similarityScore != null
                        ? (r.similarityScore <= 1 ? (r.similarityScore * 100).toFixed(1) : r.similarityScore.toFixed(1))
                        : null;
                      return (
                        <tr key={idx}>
                          <td>{idx + 1}</td>
                          <td style={{ fontWeight: 600 }}>{r.courseName || '-'}</td>
                          <td>{r.grade ?? '-'}</td>
                          <td>{r.creditHours ?? '-'}</td>
                          <td>{r.matchedCourseName || <span style={{ color: 'var(--text-muted)' }}>—</span>}</td>
                          <td>
                            {similarity != null ? (
                              <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                                <div style={{ flex: 1, height: 6, background: 'var(--border-default)', borderRadius: 4, minWidth: 60 }}>
                                  <div style={{ width: `${Math.min(similarity, 100)}%`, height: '100%', background: isEquivalent ? 'var(--success-500, #22c55e)' : 'var(--danger-500, #ef4444)', borderRadius: 4 }}></div>
                                </div>
                                <span style={{ fontSize: 12, minWidth: 38 }}>{similarity}%</span>
                              </div>
                            ) : '—'}
                          </td>
                          <td>
                            <span style={{
                              display: 'inline-block',
                              padding: '3px 12px',
                              borderRadius: 20,
                              fontSize: 12,
                              fontWeight: 700,
                              background: isEquivalent ? 'var(--success-500, #22c55e)22' : 'var(--danger-500, #ef4444)22',
                              color: isEquivalent ? 'var(--success-500, #22c55e)' : 'var(--danger-500, #ef4444)',
                              border: `1px solid ${isEquivalent ? 'var(--success-500, #22c55e)' : 'var(--danger-500, #ef4444)'}`,
                            }}>
                              {isEquivalent ? 'معادَل ✓' : 'غير معادَل ✗'}
                            </span>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </div>

            <div className="form-actions" style={{ marginTop: 20 }}>
              <button
                type="button"
                className="btn btn-outline"
                onClick={() => setSubmissionResults([])}
              >
                إخفاء النتائج
              </button>
              <button
                type="button"
                className="btn btn-primary"
                onClick={() => onNavigate('results')}
              >
                عرض جميع طلباتي
              </button>
            </div>
          </div>
        )}
    </div>
  );
}
