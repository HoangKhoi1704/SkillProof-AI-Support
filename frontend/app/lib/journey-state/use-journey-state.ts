'use client';

import { useState, useEffect, useCallback } from 'react';
import { JourneyState } from './types';
import { journeyStateRepository, createInitialJourneyState } from './session-storage-repository';

export function useJourneyState() {
  const [state, setState] = useState<JourneyState>(() => {
    if (typeof window !== 'undefined') {
      return journeyStateRepository.getState();
    }
    return createInitialJourneyState();
  });

  const [isLoaded, setIsLoaded] = useState<boolean>(false);

  useEffect(() => {
    // Initial sync
    setState(journeyStateRepository.getState());
    setIsLoaded(true);

    const handleUpdate = (e: Event) => {
      const customEvent = e as CustomEvent<JourneyState>;
      if (customEvent.detail) {
        setState(customEvent.detail);
      } else {
        setState(journeyStateRepository.getState());
      }
    };

    const handleClear = () => {
      setState(journeyStateRepository.getState());
    };

    window.addEventListener('skillproof_journey_updated', handleUpdate);
    window.addEventListener('skillproof_journey_cleared', handleClear);

    return () => {
      window.removeEventListener('skillproof_journey_updated', handleUpdate);
      window.removeEventListener('skillproof_journey_cleared', handleClear);
    };
  }, []);

  const selectRole = useCallback((roleId: string, roleTitle?: string) => {
    const updated = journeyStateRepository.selectRole(roleId, roleTitle);
    setState(updated);
    return updated;
  }, []);

  const setSelectedSkills = useCallback((skillIds: string[], mandatoryIds: string[] = []) => {
    const updated = journeyStateRepository.setSelectedSkills(skillIds, mandatoryIds);
    setState(updated);
    return updated;
  }, []);

  const setPrimaryLanguage = useCallback((languageId: string) => {
    const updated = journeyStateRepository.setPrimaryLanguage(languageId);
    setState(updated);
    return updated;
  }, []);

  const setAdaptiveSession = useCallback((session: Parameters<typeof journeyStateRepository.setAdaptiveSession>[0]) => {
    const updated = journeyStateRepository.setAdaptiveSession(session);
    setState(updated);
    return updated;
  }, []);

  const setEvaluation = useCallback((evalResult: Parameters<typeof journeyStateRepository.setEvaluation>[0], profile?: Parameters<typeof journeyStateRepository.setEvaluation>[1]) => {
    const updated = journeyStateRepository.setEvaluation(evalResult, profile);
    setState(updated);
    return updated;
  }, []);

  const setRoadmap = useCallback((roadmap: Parameters<typeof journeyStateRepository.setRoadmap>[0]) => {
    const updated = journeyStateRepository.setRoadmap(roadmap);
    setState(updated);
    return updated;
  }, []);

  const setSelectedRoadmapNode = useCallback((nodeId: string | null) => {
    const updated = journeyStateRepository.setSelectedRoadmapNode(nodeId);
    setState(updated);
    return updated;
  }, []);

  const setProject = useCallback((project: Parameters<typeof journeyStateRepository.setProject>[0]) => {
    const updated = journeyStateRepository.setProject(project);
    setState(updated);
    return updated;
  }, []);

  const setProjectEvaluation = useCallback((evalResult: Parameters<typeof journeyStateRepository.setProjectEvaluation>[0]) => {
    const updated = journeyStateRepository.setProjectEvaluation(evalResult);
    setState(updated);
    return updated;
  }, []);

  const resetJourney = useCallback(() => {
    journeyStateRepository.resetJourney();
    setState(journeyStateRepository.getState());
  }, []);

  const touch = useCallback(() => {
    journeyStateRepository.touch();
  }, []);

  return {
    state,
    isLoaded,
    selectRole,
    setSelectedSkills,
    setPrimaryLanguage,
    setAdaptiveSession,
    setEvaluation,
    setRoadmap,
    setSelectedRoadmapNode,
    setProject,
    setProjectEvaluation,
    resetJourney,
    touch
  };
}
