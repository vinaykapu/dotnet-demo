const API = '';
const COLORS = ['#6366f1','#f59e0b','#10b981','#ef4444','#3b82f6','#8b5cf6','#f97316','#06b6d4'];

let projects = [];
let tasks = [];
let selectedColor = COLORS[0];
let draggedTaskId = null;
let activeProjectFilter = '';

// ── Bootstrap ─────────────────────────────────────────────────────────────────
async function init() {
  await Promise.all([loadProjects(), loadTasks()]);
  renderSidebar();
  renderBoard();
  renderProjectsView();
  renderStats();
}

// ── API helpers ───────────────────────────────────────────────────────────────
async function api(path, method = 'GET', body = null) {
  const opts = { method, headers: { 'Content-Type': 'application/json' } };
  if (body) opts.body = JSON.stringify(body);
  const res = await fetch(API + path, opts);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.error || 'Request failed');
  }
  return res.status === 204 ? null : res.json();
}

async function loadProjects() { projects = await api('/api/projects'); }
async function loadTasks(projectId) {
  const qs = projectId ? `?projectId=${projectId}` : '';
  tasks = await api(`/api/tasks${qs}`);
}

// ── Views ─────────────────────────────────────────────────────────────────────
function showView(name) {
  document.querySelectorAll('.view').forEach(v => v.classList.remove('active'));
  document.querySelectorAll('.nav-item').forEach(b => b.classList.remove('active'));
  document.getElementById(`view-${name}`).classList.add('active');
  document.getElementById(`nav-${name}`).classList.add('active');
  if (name === 'stats') renderStats();
}

// ── Sidebar ───────────────────────────────────────────────────────────────────
function renderSidebar() {
  const el = document.getElementById('sidebar-projects');
  el.innerHTML = `<div class="sidebar-section-label">Projects</div>` +
    projects.map(p => `
      <button class="sidebar-project-item" onclick="filterByProject('${p.id}')">
        <span class="sidebar-project-dot" style="background:${p.color}"></span>
        ${p.name}
      </button>`).join('');

  // populate project filter select + task modal select
  const sel = document.getElementById('project-filter');
  const tsel = document.getElementById('task-project');
  const cur = sel.value;
  sel.innerHTML = `<option value="">All projects</option>` +
    projects.map(p => `<option value="${p.id}">${p.name}</option>`).join('');
  sel.value = cur;
  tsel.innerHTML = projects.map(p => `<option value="${p.id}">${p.name}</option>`).join('');
}

// ── Board ─────────────────────────────────────────────────────────────────────
async function filterByProject(id) {
  activeProjectFilter = id ? String(id) : '';
  document.getElementById('project-filter').value = activeProjectFilter;
  await loadTasks(activeProjectFilter || undefined);
  renderBoard();

  const proj = projects.find(p => String(p.id) === activeProjectFilter);
  document.getElementById('board-title').textContent = proj ? proj.name : 'All Projects';
}

function renderBoard() {
  const cols = { Todo: [], InProgress: [], Done: [] };
  tasks.forEach(t => {
    const key = t.status === 0 ? 'Todo' : t.status === 1 ? 'InProgress' : 'Done';
    cols[key].push(t);
  });

  ['Todo', 'InProgress', 'Done'].forEach(col => {
    const container = document.getElementById(`cards-${col.toLowerCase()}`);
    const count = document.getElementById(`count-${col.toLowerCase()}`);
    count.textContent = cols[col].length;
    container.innerHTML = cols[col].length
      ? cols[col].map(t => cardHTML(t)).join('')
      : `<div class="empty"><svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><rect x="3" y="3" width="18" height="18" rx="3"/><path d="M9 9h6M9 12h6M9 15h4"/></svg><p>No tasks here</p></div>`;
  });
}

function cardHTML(t) {
  const proj = projects.find(p => p.id === t.projectId);
  const pClass = t.priority === 2 ? 'priority-high' : t.priority === 1 ? 'priority-medium' : 'priority-low';
  const pLabel = t.priority === 2 ? 'HIGH' : t.priority === 1 ? 'MED' : 'LOW';
  return `
    <div class="card" draggable="true"
      ondragstart="onDragStart(event,${t.id})"
      ondragend="onDragEnd(event)">
      <div class="card-top">
        <span class="card-title">${esc(t.title)}</span>
        <div class="card-actions">
          <button class="card-btn delete" onclick="deleteTask(${t.id})" title="Delete">✕</button>
        </div>
      </div>
      ${t.description ? `<div class="card-desc">${esc(t.description)}</div>` : ''}
      <div class="card-footer">
        <div class="card-meta">
          ${proj ? `<span class="card-project-dot" style="background:${proj.color}"></span>
            <span class="card-project-name">${esc(proj.name)}</span>` : ''}
        </div>
        <div style="display:flex;gap:6px;align-items:center">
          <span class="${pClass}">${pLabel}</span>
          ${t.assignee ? `<span class="card-assignee">${esc(t.assignee)}</span>` : ''}
        </div>
      </div>
    </div>`;
}

// ── Drag & drop ───────────────────────────────────────────────────────────────
function onDragStart(e, id) {
  draggedTaskId = id;
  e.target.classList.add('dragging');
}
function onDragEnd(e) { e.target.classList.remove('dragging'); }
function onDragOver(e) {
  e.preventDefault();
  e.currentTarget.classList.add('drag-over');
}
async function onDrop(e, status) {
  e.currentTarget.classList.remove('drag-over');
  if (!draggedTaskId) return;
  try {
    await api(`/api/tasks/${draggedTaskId}`, 'PUT', { status });
    await loadTasks(activeProjectFilter || undefined);
    renderBoard();
    toast('Task moved to ' + status.replace('InProgress', 'In Progress'));
  } catch (err) { toast(err.message, true); }
  draggedTaskId = null;
}

// ── Task CRUD ─────────────────────────────────────────────────────────────────
function openTaskModal() {
  document.getElementById('task-title').value = '';
  document.getElementById('task-desc').value = '';
  document.getElementById('task-assignee').value = '';
  document.getElementById('task-priority').value = '1';
  if (activeProjectFilter) document.getElementById('task-project').value = activeProjectFilter;
  document.getElementById('task-modal').classList.add('open');
  document.getElementById('task-title').focus();
}

async function saveTask() {
  const title = document.getElementById('task-title').value.trim();
  const projectId = parseInt(document.getElementById('task-project').value);
  if (!title) { toast('Title is required', true); return; }
  try {
    await api('/api/tasks', 'POST', {
      title,
      description: document.getElementById('task-desc').value.trim(),
      projectId,
      priority: parseInt(document.getElementById('task-priority').value),
      assignee: document.getElementById('task-assignee').value.trim(),
    });
    closeModal('task-modal');
    await loadTasks(activeProjectFilter || undefined);
    renderBoard();
    toast('Task created!');
  } catch (err) { toast(err.message, true); }
}

async function deleteTask(id) {
  if (!confirm('Delete this task?')) return;
  try {
    await api(`/api/tasks/${id}`, 'DELETE');
    await loadTasks(activeProjectFilter || undefined);
    renderBoard();
    renderStats();
    toast('Task deleted');
  } catch (err) { toast(err.message, true); }
}

// ── Projects ──────────────────────────────────────────────────────────────────
function openProjectModal() {
  document.getElementById('proj-name').value = '';
  document.getElementById('proj-desc').value = '';
  selectedColor = COLORS[0];
  const swatches = document.getElementById('color-swatches');
  swatches.innerHTML = COLORS.map((c, i) =>
    `<div class="swatch${i === 0 ? ' selected' : ''}" style="background:${c}"
      onclick="selectColor(this,'${c}')"></div>`).join('');
  document.getElementById('project-modal').classList.add('open');
  document.getElementById('proj-name').focus();
}

function selectColor(el, color) {
  document.querySelectorAll('.swatch').forEach(s => s.classList.remove('selected'));
  el.classList.add('selected');
  selectedColor = color;
}

async function saveProject() {
  const name = document.getElementById('proj-name').value.trim();
  if (!name) { toast('Name is required', true); return; }
  try {
    await api('/api/projects', 'POST', {
      name,
      description: document.getElementById('proj-desc').value.trim(),
      color: selectedColor,
    });
    closeModal('project-modal');
    await loadProjects();
    renderSidebar();
    renderProjectsView();
    toast('Project created!');
  } catch (err) { toast(err.message, true); }
}

async function deleteProject(id) {
  if (!confirm('Delete this project and all its tasks?')) return;
  try {
    await api(`/api/projects/${id}`, 'DELETE');
    if (activeProjectFilter === String(id)) {
      activeProjectFilter = '';
      document.getElementById('project-filter').value = '';
    }
    await Promise.all([loadProjects(), loadTasks()]);
    renderSidebar();
    renderBoard();
    renderProjectsView();
    renderStats();
    toast('Project deleted');
  } catch (err) { toast(err.message, true); }
}

function renderProjectsView() {
  const grid = document.getElementById('projects-grid');
  if (!projects.length) { grid.innerHTML = '<div class="empty"><p>No projects yet</p></div>'; return; }
  grid.innerHTML = projects.map(p => {
    const pt = tasks.filter(t => t.projectId === p.id);
    const done = pt.filter(t => t.status === 2).length;
    return `
      <div class="project-card" onclick="filterByProject(${p.id});showView('board')">
        <div class="project-card-accent" style="background:${p.color}"></div>
        <div class="project-card-header">
          <span class="project-card-name">${esc(p.name)}</span>
          <button class="project-delete-btn" onclick="event.stopPropagation();deleteProject(${p.id})" title="Delete">✕</button>
        </div>
        <div class="project-card-desc">${esc(p.description) || 'No description'}</div>
        <div class="project-card-stats">
          <span class="project-stat"><strong>${pt.length}</strong> tasks</span>
          <span class="project-stat"><strong>${done}</strong> done</span>
          <span class="project-stat"><strong>${pt.length - done}</strong> remaining</span>
        </div>
      </div>`;
  }).join('');
}

// ── Stats ─────────────────────────────────────────────────────────────────────
async function renderStats() {
  try {
    const s = await api('/api/stats');
    const pct = s.total ? Math.round(s.done / s.total * 100) : 0;
    document.getElementById('stats-grid').innerHTML = `
      <div class="stat-card"><div class="stat-label">Total Tasks</div><div class="stat-value c-slate">${s.total}</div><div class="stat-sub">across ${projects.length} projects</div></div>
      <div class="stat-card"><div class="stat-label">To Do</div><div class="stat-value c-indigo">${s.todo}</div><div class="stat-sub">not yet started</div></div>
      <div class="stat-card"><div class="stat-label">In Progress</div><div class="stat-value c-amber">${s.inProgress}</div><div class="stat-sub">being worked on</div></div>
      <div class="stat-card"><div class="stat-label">Done</div><div class="stat-value c-green">${s.done}</div><div class="stat-sub">${pct}% completion rate</div></div>
      <div class="stat-card"><div class="stat-label">High Priority</div><div class="stat-value c-red">${s.highPriority}</div><div class="stat-sub">need attention</div></div>
      <div class="stat-card"><div class="stat-label">Projects</div><div class="stat-value c-indigo">${projects.length}</div><div class="stat-sub">active workstreams</div></div>`;
  } catch (e) { /* ignore */ }
}

// ── Utilities ─────────────────────────────────────────────────────────────────
function closeModal(id) { document.getElementById(id).classList.remove('open'); }
function esc(str) { return (str || '').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;'); }
let toastTimer;
function toast(msg, err = false) {
  const el = document.getElementById('toast');
  el.textContent = msg;
  el.style.background = err ? '#ef4444' : '#0f172a';
  el.classList.add('show');
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => el.classList.remove('show'), 2800);
}

// keyboard shortcuts
document.addEventListener('keydown', e => {
  if (e.key === 'Escape') {
    document.querySelectorAll('.modal-backdrop.open').forEach(m => m.classList.remove('open'));
  }
});

// remove drag-over on leave
document.querySelectorAll('.column').forEach(col => {
  col.addEventListener('dragleave', e => {
    if (!col.contains(e.relatedTarget)) col.classList.remove('drag-over');
  });
});

init();
