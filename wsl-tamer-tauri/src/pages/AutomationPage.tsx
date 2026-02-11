// Automation Page - Profile Automation Rules

import { useState, useEffect } from 'react';
import type { AutomationRule, TriggerType, WslProfile } from '../types';
import { toErrorMessage } from '../utils/errorUtils';
import { automationService, profileService } from '../services';
import { useConfirm } from '../contexts/ConfirmContext';

// Generate unique ID using platform crypto
function generateId(): string {
  return crypto.randomUUID();
}

// Trigger type descriptions
const triggerDescriptions: Record<TriggerType, { label: string; placeholder: string; icon: string; description: string }> = {
  Time: {
    label: 'Scheduled Time',
    placeholder: 'HH:MM (24-hour format)',
    icon: '⏰',
    description: 'Activate profile at a specific time each day'
  },
  Process: {
    label: 'Process Running',
    placeholder: 'Process name (e.g., code.exe)',
    icon: '⚙️',
    description: 'Activate when a specific process starts'
  },
  PowerState: {
    label: 'Power State',
    placeholder: 'AC or Battery',
    icon: '🔋',
    description: 'Activate when power source changes'
  },
  Network: {
    label: 'Network Connected',
    placeholder: 'Network name or SSID',
    icon: '📶',
    description: 'Activate when connected to a specific network'
  }
};

export default function AutomationPage() {
  const [rules, setRules] = useState<AutomationRule[]>([]);
  const [profiles, setProfiles] = useState<WslProfile[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const confirm = useConfirm();
  
  // Edit state
  const [editingRule, setEditingRule] = useState<AutomationRule | null>(null);
  const [isCreating, setIsCreating] = useState(false);

  // Load rules and profiles on mount
  useEffect(() => {
    loadData();
  }, []);

  async function loadData() {
    try {
      setLoading(true);
      setError(null);
      
      const [rulesData, profilesData] = await Promise.all([
        profileService.getRules(),
        profileService.getProfiles()
      ]);
      
      setRules(rulesData);
      setProfiles(profilesData);
    } catch (err: unknown) {
      setError(`Failed to load data: ${toErrorMessage(err)}`);
    } finally {
      setLoading(false);
    }
  }

  async function handleSaveRule(rule: AutomationRule) {
    try {
      await profileService.saveRule(rule);
      setSuccess('Rule saved successfully!');
      setTimeout(() => setSuccess(null), 3000);
      setEditingRule(null);
      setIsCreating(false);
      await loadData();
    } catch (err: unknown) {
      setError(`Failed to save rule: ${toErrorMessage(err)}`);
    }
  }

  async function handleDeleteRule(id: string) {
    const ok = await confirm({ title: 'Delete Rule', message: 'Are you sure you want to delete this rule?', danger: true });
    if (!ok) return;
    
    try {
      await profileService.deleteRule(id);
      setSuccess('Rule deleted!');
      setTimeout(() => setSuccess(null), 3000);
      await loadData();
    } catch (err: unknown) {
      setError(`Failed to delete rule: ${toErrorMessage(err)}`);
    }
  }

  async function handleToggleRule(id: string) {
    try {
      await profileService.toggleRule(id);
      await loadData();
    } catch (err: unknown) {
      setError(`Failed to toggle rule: ${toErrorMessage(err)}`);
    }
  }

  function handleCreateNew() {
    const newRule: AutomationRule = {
      id: generateId(),
      name: '',
      isEnabled: true,
      triggerType: 'Time',
      triggerValue: '',
      targetProfileId: profiles[0]?.id || ''
    };
    setEditingRule(newRule);
    setIsCreating(true);
  }

  function handleEditRule(rule: AutomationRule) {
    setEditingRule({ ...rule });
    setIsCreating(false);
  }

  function handleCancelEdit() {
    setEditingRule(null);
    setIsCreating(false);
  }

  if (loading) {
    return (
      <div className="page-content">
        <div className="loading-container">
          <div className="loading-spinner"></div>
          <p>Loading automation rules...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="page-content">
      <div className="page-header">
        <h1>Automation</h1>
        <p className="page-description">
          Set up rules to automatically switch profiles based on triggers
        </p>
      </div>

      {error && (
        <div className="alert alert-error">
          <span className="alert-icon">⚠️</span>
          <span>{error}</span>
          <button className="alert-dismiss" onClick={() => setError(null)}>×</button>
        </div>
      )}

      {success && (
        <div className="alert alert-success">
          <span className="alert-icon">✓</span>
          <span>{success}</span>
          <button className="alert-dismiss" onClick={() => setSuccess(null)}>×</button>
        </div>
      )}

      {/* Rule Editor Modal */}
      {editingRule && (
        <RuleEditor
          rule={editingRule}
          profiles={profiles}
          isNew={isCreating}
          onSave={handleSaveRule}
          onCancel={handleCancelEdit}
        />
      )}

      {/* Toolbar */}
      <div className="automation-toolbar">
        <button className="btn btn-primary" onClick={handleCreateNew}>
          <span>➕</span> New Rule
        </button>
      </div>

      {/* Rules List */}
      {rules.length === 0 ? (
        <div className="empty-state card">
          <div className="empty-icon">🤖</div>
          <h3>No automation rules yet</h3>
          <p>Create rules to automatically switch WSL profiles based on time, running processes, or power state.</p>
          <button className="btn btn-primary" onClick={handleCreateNew}>
            Create Your First Rule
          </button>
        </div>
      ) : (
        <div className="rules-list">
          {rules.map(rule => (
            <RuleCard
              key={rule.id}
              rule={rule}
              profile={profiles.find(p => p.id === rule.targetProfileId)}
              onToggle={() => handleToggleRule(rule.id)}
              onEdit={() => handleEditRule(rule)}
              onDelete={() => handleDeleteRule(rule.id)}
            />
          ))}
        </div>
      )}

      {/* Help Section */}
      <div className="help-text">
        <h4>💡 How Automation Works</h4>
        <ul>
          <li><strong>Time-based:</strong> Switch profiles at scheduled times (e.g., "Power Saver" at night)</li>
          <li><strong>Process-based:</strong> Activate "Performance" when resource-heavy apps start</li>
          <li><strong>Power-based:</strong> Use "Battery Saver" when on battery power</li>
          <li><strong>Network-based:</strong> Apply "Work" profile when connected to office network</li>
        </ul>
      </div>
    </div>
  );
}

// Rule Card Component
interface RuleCardProps {
  rule: AutomationRule;
  profile?: WslProfile;
  onToggle: () => void;
  onEdit: () => void;
  onDelete: () => void;
}

function RuleCard({ rule, profile, onToggle, onEdit, onDelete }: RuleCardProps) {
  const trigger = triggerDescriptions[rule.triggerType];
  
  return (
    <div className={`rule-card ${rule.isEnabled ? 'enabled' : 'disabled'}`}>
      <div className="rule-header">
        <div className="rule-trigger-icon">{trigger.icon}</div>
        <div className="rule-info">
          <h3>{rule.name || 'Unnamed Rule'}</h3>
          <div className="rule-meta">
            <span className="trigger-type">{trigger.label}</span>
            <span className="trigger-value">{rule.triggerValue}</span>
          </div>
        </div>
        <div className="rule-toggle">
          <input
            type="checkbox"
            checked={rule.isEnabled}
            onChange={onToggle}
            className="toggle-switch"
          />
        </div>
      </div>
      
      <div className="rule-target">
        <span className="target-label">Applies:</span>
        <span className="target-profile">{profile?.name || 'Unknown Profile'}</span>
      </div>
      
      <div className="rule-actions">
        <button className="btn btn-sm btn-secondary" onClick={onEdit}>
          ✏️ Edit
        </button>
        <button className="btn btn-sm btn-danger" onClick={onDelete}>
          🗑️ Delete
        </button>
      </div>
    </div>
  );
}

// Rule Editor Component
interface RuleEditorProps {
  rule: AutomationRule;
  profiles: WslProfile[];
  isNew: boolean;
  onSave: (rule: AutomationRule) => void;
  onCancel: () => void;
}

function RuleEditor({ rule, profiles, isNew, onSave, onCancel }: RuleEditorProps) {
  const [formData, setFormData] = useState<AutomationRule>(rule);
  const [errors, setErrors] = useState<Record<string, string>>({});

  function handleChange<K extends keyof AutomationRule>(key: K, value: AutomationRule[K]) {
    setFormData(prev => ({ ...prev, [key]: value }));
    // Clear error when field changes
    if (errors[key]) {
      setErrors(prev => {
        const next = { ...prev };
        delete next[key];
        return next;
      });
    }
  }

  function validate(): boolean {
    const newErrors = automationService.validateRule(formData);
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (validate()) {
      onSave(formData);
    }
  }

  const trigger = triggerDescriptions[formData.triggerType];

  return (
    <div className="modal-overlay" onClick={onCancel}>
      <div className="modal rule-editor" onClick={e => e.stopPropagation()}>
        <h2>{isNew ? 'Create New Rule' : 'Edit Rule'}</h2>
        
        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="ruleName">Rule Name</label>
            <input
              id="ruleName"
              type="text"
              value={formData.name}
              onChange={e => handleChange('name', e.target.value)}
              placeholder="e.g., Night Mode, Heavy Workload"
              className={errors.name ? 'error' : ''}
            />
            {errors.name && <span className="form-error">{errors.name}</span>}
          </div>

          <div className="form-group">
            <label htmlFor="triggerType">Trigger Type</label>
            <select
              id="triggerType"
              value={formData.triggerType}
              onChange={e => handleChange('triggerType', e.target.value as TriggerType)}
            >
              {(Object.keys(triggerDescriptions) as TriggerType[]).map(type => (
                <option key={type} value={type}>
                  {triggerDescriptions[type].icon} {triggerDescriptions[type].label}
                </option>
              ))}
            </select>
            <p className="form-hint">{trigger.description}</p>
          </div>

          <div className="form-group">
            <label htmlFor="triggerValue">Trigger Value</label>
            {formData.triggerType === 'PowerState' ? (
              <select
                id="triggerValue"
                value={formData.triggerValue}
                onChange={e => handleChange('triggerValue', e.target.value)}
                className={errors.triggerValue ? 'error' : ''}
              >
                <option value="">Select power state...</option>
                <option value="AC">AC Power (Plugged In)</option>
                <option value="Battery">Battery Power</option>
              </select>
            ) : (
              <input
                id="triggerValue"
                type="text"
                value={formData.triggerValue}
                onChange={e => handleChange('triggerValue', e.target.value)}
                placeholder={trigger.placeholder}
                className={errors.triggerValue ? 'error' : ''}
              />
            )}
            {errors.triggerValue && <span className="form-error">{errors.triggerValue}</span>}
          </div>

          <div className="form-group">
            <label htmlFor="targetProfile">Target Profile</label>
            <select
              id="targetProfile"
              value={formData.targetProfileId}
              onChange={e => handleChange('targetProfileId', e.target.value)}
              className={errors.targetProfileId ? 'error' : ''}
            >
              <option value="">Select profile...</option>
              {profiles.map(profile => (
                <option key={profile.id} value={profile.id}>
                  {profile.name} ({profile.memory}, {profile.processors} CPUs)
                </option>
              ))}
            </select>
            {errors.targetProfileId && <span className="form-error">{errors.targetProfileId}</span>}
          </div>

          <div className="form-group checkbox">
            <label>
              <input
                type="checkbox"
                checked={formData.isEnabled}
                onChange={e => handleChange('isEnabled', e.target.checked)}
              />
              Enable this rule
            </label>
          </div>

          <div className="form-actions">
            <button type="button" className="btn btn-secondary" onClick={onCancel}>
              Cancel
            </button>
            <button type="submit" className="btn btn-primary">
              {isNew ? 'Create Rule' : 'Save Changes'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
