using System;
using System.Collections.Generic;
using MyFolder._1._Scripts._6._GlobalQuest;
using UnityEngine;

namespace MyFolder._1._Scripts._1._UI._0._GameStage._1._StageUI._0._Quest
{
	public sealed class GlobalQuestUIController : MonoBehaviour
	{
		[Header("Parents")]
		[SerializeField] private Transform container;

		[Header("Prefabs")]
		[SerializeField] private LineQuestPanel lineQuestPanelPrefab;
		[SerializeField] private CircleQuestPanel circleQuestPanelPrefab;

		private readonly Dictionary<int, QuestPanel> questIdToPanel = new();
		private readonly Dictionary<int, GlobalQuestReplicator> questIdToRep = new();


		private void OnEnable()
		{
			GlobalQuestReplicator.OnReplicatorSpawned += OnReplicatorSpawned;
			GlobalQuestReplicator.OnReplicatorDespawned += OnReplicatorDespawned;

			// 이미 씬에 존재하는 Replicator들 초기 동기화
			InitializeExistingReplicators();
		}

		private void OnDisable()
		{
			GlobalQuestReplicator.OnReplicatorSpawned -= OnReplicatorSpawned;
			GlobalQuestReplicator.OnReplicatorDespawned -= OnReplicatorDespawned;

			questIdToRep.Clear();

			foreach (var kv in questIdToPanel)
				if (kv.Value) Destroy(kv.Value.gameObject);
			questIdToPanel.Clear();

		}

		private void OnReplicatorSpawned(GlobalQuestReplicator rep)
		{
			int questId = rep.QuestId.Value;
			if (questIdToPanel.ContainsKey(questId))
				return;

			QuestPanel panel = CreatePanelFor(rep);
			if (!panel)
				return;

			panel.Initialize(rep.QuestName.Value);

			questIdToPanel[questId] = panel;
			questIdToRep[questId] = rep;

			ApplyAll(rep);
		}

		private void OnReplicatorDespawned(GlobalQuestReplicator rep)
		{
			int questId = rep.QuestId.Value;
			CleanupQuest(questId);
		}

		private void InitializeExistingReplicators()
		{
			var reps = FindObjectsOfType<GlobalQuestReplicator>();
			for (int i = 0; i < reps.Length; i++)
			{
				var rep = reps[i];
				if (!rep)
					continue;
				OnReplicatorSpawned(rep);
			}
		}

		private QuestPanel CreatePanelFor(GlobalQuestReplicator rep)
		{
			QuestPanel prefab = SelectPanelPrefab(rep.QuestType.Value);
			if (!prefab)
				return null;
			return Instantiate(prefab, container ? container : transform);
		}

		private QuestPanel SelectPanelPrefab(GlobalQuestType type)
		{
			switch (type)
			{
				case GlobalQuestType.Extermination:
					return lineQuestPanelPrefab;
				case GlobalQuestType.Defense:
				case GlobalQuestType.Survival:
					return circleQuestPanelPrefab;
				default:
					return lineQuestPanelPrefab;
			}
		}


		private void CleanupQuest(int questId)
		{
			if (questIdToRep.TryGetValue(questId, out var rep))
			{
				questIdToRep.Remove(questId);
			}

			if (questIdToPanel.TryGetValue(questId, out var panel))
			{
				if (panel) Destroy(panel.gameObject);
				questIdToPanel.Remove(questId);
			}
		}


		private void LateUpdate()
		{
			// 폴링 방식으로 모든 Replicator 상태를 UI에 반영
			// 디스폰 또는 종료 시 클린업
			if (questIdToRep.Count == 0)
				return;

			// 복사본으로 순회: 중간에 컬렉션이 수정될 수 있음
			var reps = new List<GlobalQuestReplicator>(questIdToRep.Values);
			for (int i = 0; i < reps.Count; i++)
			{
				var rep = reps[i];
				if (!rep)
					continue;

				if (rep.IsEnd.Value)
				{
					CleanupQuest(rep.QuestId.Value);
					continue;
				}

				// 패널이 누락되어 있으면 복구
				int qid = rep.QuestId.Value;
				if (!questIdToPanel.ContainsKey(qid))
				{
					OnReplicatorSpawned(rep);
					continue;
				}

				ApplyAll(rep);
			}
		}

		private void ApplyAll(GlobalQuestReplicator rep)
		{
			OnQuestNameChanged(rep, rep.QuestName.Value);
			switch (rep.QuestType.Value)
			{
				case GlobalQuestType.Extermination:
					OnProgressOrTargetChanged(rep);
					break;
				case GlobalQuestType.Defense:
				case GlobalQuestType.Survival:
					OnTimeChanged(rep);
					break;
			}
		}

		private void OnQuestNameChanged(GlobalQuestReplicator rep, string nextName)
		{
			int questId = rep.QuestId.Value;
			if (!questIdToPanel.TryGetValue(questId, out var panel) || !panel)
				return;

			panel.Initialize(nextName);
		}

		private void OnProgressOrTargetChanged(GlobalQuestReplicator rep)
		{
			int questId = rep.QuestId.Value;
			if (!questIdToPanel.TryGetValue(questId, out var panel) || !panel)
				return;

			float current = rep.Progress.Value;
			float max = Mathf.Max(1f, rep.Target.Value);
			panel.ProgressUpdate(current, max);
		}

		private void OnTimeChanged(GlobalQuestReplicator rep)
		{
			int questId = rep.QuestId.Value;
			if (!questIdToPanel.TryGetValue(questId, out var panel) || !panel)
				return;

			float current = rep.ElapsedTime.Value;
			float max = Mathf.Max(0.01f, rep.LimitTime.Value);
			panel.TimeUpdate(current, max);
		}
	}
}


