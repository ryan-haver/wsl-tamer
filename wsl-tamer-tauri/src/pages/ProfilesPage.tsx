// Profiles Page - Manage resource profiles

import { useEffect, useState } from 'react';
import { profileService } from '../services';
import { useToast } from '../contexts/ToastContext';
import { useConfirm } from '../contexts/ConfirmContext';
import { toErrorMessage } from '../utils/errorUtils';
import type { WslProfile } from '../types';

export function ProfilesPage({ onUnsavedChanges }: { onUnsavedChanges?: (hasChanges: boolean) => void }) {
  const { showToast } = useToast();
  const confirm = useConfirm();
  const [profiles, setProfiles] = useState<WslProfile[]>([]);
  const [currentProfile, setCurrentProfile] = useState<WslProfile | null>(null);
  const [editingProfile, setEditingProfile] = useState<WslProfile | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadProfiles();
  }, []);

  const loadProfiles = async () => {
    try {
      const [allProfiles, current] = await Promise.all([
        profileService.getProfiles(),
        profileService.getCurrentProfile()
      ]);
      setProfiles(allProfiles);
      setCurrentProfile(current);
    } catch (error) {
      console.error('Failed to load profiles:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleApply = async (id: string) => {
    try {
      await profileService.applyProfile(id);
      showToast('success', 'Profile applied! Restart WSL for changes to take effect.');
      await loadProfiles();
    } catch (error) {
      showToast('error', 'Failed to apply profile: ' + toErrorMessage(error));
    }
  };

  const handleEdit = (profile: WslProfile) => {
    setEditingProfile({ ...profile });
    onUnsavedChanges?.(true);
  };

  const handleSave = async () => {
    if (!editingProfile) return;
    try {
      await profileService.saveProfile(editingProfile);
      setEditingProfile(null);
      onUnsavedChanges?.(false);
      await loadProfiles();
    } catch (error) {
      showToast('error', 'Failed to save profile: ' + toErrorMessage(error));
    }
  };

  const handleDelete = async (id: string) => {
    const ok = await confirm({ title: 'Delete Profile', message: 'Are you sure you want to delete this profile?', danger: true });
    if (!ok) return;
    try {
      await profileService.deleteProfile(id);
      await loadProfiles();
    } catch (error) {
      showToast('error', 'Failed to delete profile: ' + toErrorMessage(error));
    }
  };

  const handleAddNew = () => {
    const newProfile: WslProfile = {
      id: crypto.randomUUID(),
      name: 'New Profile',
      memory: '4GB',
      processors: 2,
      swap: '2GB',
      localhostForwarding: true,
      networkingMode: 'NAT',
      guiApplications: true,
      debugConsole: false
    };
    setEditingProfile(newProfile);
    onUnsavedChanges?.(true);
  };

  const handleCancelEdit = () => {
    setEditingProfile(null);
    onUnsavedChanges?.(false);
  };

  return (
    <div className="page">
      <header className="page-header">
        <h1>Profiles</h1>
        <p>Manage WSL resource profiles</p>
      </header>

      <div className="toolbar">
        <button onClick={handleAddNew} className="btn btn-primary">
          ➕ Add Profile
        </button>
      </div>

      {/* Profile Editor */}
      {editingProfile && (
        <section className="card editor">
          <h2>{profiles.find(p => p.id === editingProfile.id) ? 'Edit Profile' : 'New Profile'}</h2>
          <div className="form-grid">
            <div className="form-group">
              <label>Name</label>
              <input
                type="text"
                value={editingProfile.name}
                onChange={e => setEditingProfile({ ...editingProfile, name: e.target.value })}
              />
            </div>
            <div className="form-group">
              <label>Memory (e.g., 4GB, 8GB)</label>
              <input
                type="text"
                value={editingProfile.memory}
                onChange={e => setEditingProfile({ ...editingProfile, memory: e.target.value })}
              />
            </div>
            <div className="form-group">
              <label>Processors</label>
              <input
                type="number"
                min="1"
                max="64"
                value={editingProfile.processors}
                onChange={e => setEditingProfile({ ...editingProfile, processors: parseInt(e.target.value) || 1 })}
              />
            </div>
            <div className="form-group">
              <label>Swap (e.g., 0, 2GB)</label>
              <input
                type="text"
                value={editingProfile.swap}
                onChange={e => setEditingProfile({ ...editingProfile, swap: e.target.value })}
              />
            </div>
            <div className="form-group">
              <label>Networking Mode</label>
              <select
                value={editingProfile.networkingMode}
                onChange={e => setEditingProfile({ ...editingProfile, networkingMode: e.target.value })}
              >
                <option value="NAT">NAT</option>
                <option value="mirrored">Mirrored</option>
                <option value="bridged">Bridged</option>
              </select>
            </div>
            <div className="form-group checkbox">
              <label>
                <input
                  type="checkbox"
                  checked={editingProfile.localhostForwarding}
                  onChange={e => setEditingProfile({ ...editingProfile, localhostForwarding: e.target.checked })}
                />
                Localhost Forwarding
              </label>
            </div>
            <div className="form-group checkbox">
              <label>
                <input
                  type="checkbox"
                  checked={editingProfile.guiApplications}
                  onChange={e => setEditingProfile({ ...editingProfile, guiApplications: e.target.checked })}
                />
                GUI Applications (WSLg)
              </label>
            </div>
          </div>
          <div className="form-actions">
            <button onClick={handleSave} className="btn btn-success">
              💾 Save
            </button>
            <button onClick={handleCancelEdit} className="btn btn-secondary">
              Cancel
            </button>
          </div>
        </section>
      )}

      {/* Profiles List */}
      <section className="card">
        <h2>Available Profiles</h2>
        {loading ? (
          <div className="loading">Loading...</div>
        ) : profiles.length === 0 ? (
          <p className="empty-state">No profiles configured.</p>
        ) : (
          <div className="profiles-grid">
            {profiles.map(profile => (
              <div 
                key={profile.id} 
                className={`profile-card ${currentProfile?.id === profile.id ? 'active' : ''}`}
              >
                <div className="profile-header">
                  <h3>{profile.name}</h3>
                  {currentProfile?.id === profile.id && (
                    <span className="active-badge">Active</span>
                  )}
                </div>
                <div className="profile-specs">
                  <span>💾 {profile.memory}</span>
                  <span>🖥️ {profile.processors} CPUs</span>
                  <span>📀 {profile.swap} swap</span>
                </div>
                <div className="profile-actions">
                  <button onClick={() => handleApply(profile.id)} className="btn btn-sm btn-primary">
                    Apply
                  </button>
                  <button onClick={() => handleEdit(profile)} className="btn btn-sm btn-secondary">
                    Edit
                  </button>
                  <button onClick={() => handleDelete(profile.id)} className="btn btn-sm btn-danger">
                    Delete
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}

export default ProfilesPage;
