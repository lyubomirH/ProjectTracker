// WorkItems AJAX functions

async function updateWorkItemStatus(workItemId, newStatus) {
    try {
        const response = await fetch(`/api/WorkItems/${workItemId}/status`, {
            method: 'PATCH',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            },
            body: JSON.stringify({ status: newStatus })
        });

        if (response.ok) {
            location.reload();
        } else {
            const error = await response.text();
            showToast('Error updating status: ' + error, 'danger');
        }
    } catch (error) {
        console.error('Error:', error);
        showToast('An error occurred while updating status', 'danger');
    }
}

async function addQuickComment(workItemId, content) {
    if (!content.trim()) {
        showToast('Please enter a comment', 'warning');
        return false;
    }

    try {
        const response = await fetch(`/api/WorkItems/${workItemId}/comments`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            },
            body: JSON.stringify({ content: content })
        });

        if (response.ok) {
            const result = await response.json();
            if (result.success) {
                showToast('Comment added successfully!', 'success');
                return true;
            }
        } else {
            const error = await response.text();
            showToast('Error adding comment: ' + error, 'danger');
        }
    } catch (error) {
        console.error('Error:', error);
        showToast('An error occurred while adding comment', 'danger');
    }
    return false;
}

async function loadProjectStatistics(projectId) {
    try {
        const response = await fetch(`/api/Projects/${projectId}/statistics`);

        if (response.ok) {
            const stats = await response.json();
            updateStatisticsModal(stats);
        } else {
            showToast('Error loading statistics', 'danger');
        }
    } catch (error) {
        console.error('Error:', error);
        showToast('An error occurred while loading statistics', 'danger');
    }
}

function showToast(message, type) {
    const toastContainer = document.getElementById('toastContainer');
    if (!toastContainer) return;

    const toast = document.createElement('div');
    toast.className = `toast align-items-center text-white bg-${type} border-0`;
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');

    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                ${message}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>
    `;

    toastContainer.appendChild(toast);
    const bsToast = new bootstrap.Toast(toast);
    bsToast.show();

    toast.addEventListener('hidden.bs.toast', () => toast.remove());
}

function updateStatisticsModal(stats) {
    document.getElementById('statProjectName').textContent = stats.name;
    document.getElementById('statCompletion').textContent = `${stats.completionPercentage.toFixed(1)}%`;
    document.getElementById('statTotal').textContent = stats.workItemsCount;
    document.getElementById('statCompleted').textContent = stats.completedWorkItemsCount;
    document.getElementById('statToDo').textContent = stats.toDoCount;
    document.getElementById('statInProgress').textContent = stats.inProgressCount;
    document.getElementById('statBlocked').textContent = stats.blockedCount;
    document.getElementById('statTeamMembers').textContent = stats.teamMembersCount;
}