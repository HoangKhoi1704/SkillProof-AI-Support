import { test, describe, beforeEach } from 'node:test';
import assert from 'node:assert/strict';

import {
  CURRENT_JOURNEY_SCHEMA_VERSION,
  JOURNEY_TTL_MS
} from '../app/lib/journey-state/types';
import {
  SessionStorageJourneyStateRepository,
  STORAGE_KEY,
  createInitialJourneyState
} from '../app/lib/journey-state/session-storage-repository';

// Mock browser window and sessionStorage
class MockSessionStorage {
  private store: Map<string, string> = new Map();

  getItem(key: string): string | null {
    return this.store.get(key) || null;
  }
  setItem(key: string, value: string): void {
    this.store.set(key, String(value));
  }
  removeItem(key: string): void {
    this.store.delete(key);
  }
  clear(): void {
    this.store.clear();
  }
}

describe('SessionStorageJourneyStateRepository', () => {
  let repo: SessionStorageJourneyStateRepository;
  let mockStorage: MockSessionStorage;

  beforeEach(() => {
    mockStorage = new MockSessionStorage();
    (global as any).window = {
      sessionStorage: mockStorage,
      dispatchEvent: () => true
    };
    (global as any).CustomEvent = class {
      type: string;
      detail?: any;
      constructor(type: string, eventInitDict?: { detail?: any }) {
        this.type = type;
        this.detail = eventInitDict?.detail;
      }
    };
    repo = new SessionStorageJourneyStateRepository();
  });

  test('createInitialJourneyState returns empty state with 30-min TTL', () => {
    const state = createInitialJourneyState();
    assert.equal(state.schemaVersion, CURRENT_JOURNEY_SCHEMA_VERSION);
    assert.equal(state.selectedRoleId, null);
    assert.deepEqual(state.userSelectedSkillIds, []);
    assert.ok(state.expiresAt - state.createdAt >= JOURNEY_TTL_MS - 100);
  });

  test('selectRole stores role and invalidates downstream state on change', () => {
    repo.selectRole('backend-developer', 'Backend Developer');
    let state = repo.getState();
    assert.equal(state.selectedRoleId, 'backend-developer');
    assert.equal(state.selectedRoleTitle, 'Backend Developer');

    // Add skills and adaptive session
    repo.setSelectedSkills(['shared.sql', 'backend.rest-apis']);
    repo.saveState({ adaptiveSessionId: 'sess-123' });
    state = repo.getState();
    assert.equal(state.userSelectedSkillIds.length, 2);
    assert.equal(state.adaptiveSessionId, 'sess-123');

    // Change role -> must invalidate downstream state
    repo.selectRole('frontend-developer', 'Frontend Developer');
    state = repo.getState();
    assert.equal(state.selectedRoleId, 'frontend-developer');
    assert.deepEqual(state.userSelectedSkillIds, []);
    assert.equal(state.adaptiveSessionId, null);
  });

  test('setSelectedSkills invalidates downstream assessment if skills change', () => {
    repo.selectRole('backend-developer');
    repo.setSelectedSkills(['backend.rest-apis']);
    repo.saveState({ adaptiveSessionId: 'sess-456' });

    assert.equal(repo.getState().adaptiveSessionId, 'sess-456');

    // Change skills
    repo.setSelectedSkills(['backend.rest-apis', 'backend.testing']);
    const state = repo.getState();
    assert.deepEqual(state.userSelectedSkillIds, ['backend.rest-apis', 'backend.testing']);
    assert.equal(state.adaptiveSessionId, null);
  });

  test('sliding TTL refreshes expiresAt on meaningful updates', () => {
    repo.selectRole('backend-developer');
    const initialExpiry = repo.getState().expiresAt;

    // Simulate clock progression
    const futureTime = Date.now() + 5000;
    const originalNow = Date.now;
    Date.now = () => futureTime;

    try {
      repo.setSelectedSkills(['shared.sql']);
      const updatedExpiry = repo.getState().expiresAt;
      assert.ok(updatedExpiry > initialExpiry, 'Sliding TTL should push expiration forward');
    } finally {
      Date.now = originalNow;
    }
  });

  test('expired journey state (>30 minutes) clears storage safely', () => {
    repo.selectRole('backend-developer');
    const state = repo.getState();
    // Force expired time
    state.expiresAt = Date.now() - 1000;
    mockStorage.setItem(STORAGE_KEY, JSON.stringify(state));

    const retrieved = repo.getState();
    assert.equal(retrieved.selectedRoleId, null, 'Expired state should reset to empty initial state');
  });

  test('corrupted JSON recovery resets to clean state without throwing', () => {
    mockStorage.setItem(STORAGE_KEY, '{"invalid_json: true');
    const state = repo.getState();
    assert.equal(state.selectedRoleId, null);
    assert.equal(state.schemaVersion, CURRENT_JOURNEY_SCHEMA_VERSION);
  });

  test('schema version mismatch resets to clean state', () => {
    const oldState = createInitialJourneyState();
    oldState.schemaVersion = 1; // old version
    oldState.selectedRoleId = 'backend-developer';
    mockStorage.setItem(STORAGE_KEY, JSON.stringify(oldState));

    const state = repo.getState();
    assert.equal(state.selectedRoleId, null);
    assert.equal(state.schemaVersion, CURRENT_JOURNEY_SCHEMA_VERSION);
  });

  test('resetJourney clears all state', () => {
    repo.selectRole('backend-developer');
    repo.setSelectedSkills(['shared.sql']);
    repo.resetJourney();

    const state = repo.getState();
    assert.equal(state.selectedRoleId, null);
    assert.deepEqual(state.userSelectedSkillIds, []);
  });
});
