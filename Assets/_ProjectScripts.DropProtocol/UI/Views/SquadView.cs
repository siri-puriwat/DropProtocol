using UnityEngine;

namespace DropProtocol
{
/// <summary>One row per spawned player. Four entries are cheap to poll, so there is no spawn event plumbing.</summary>
public sealed class SquadView : HudView
{
    [SerializeField]
    private SquadRowView[] m_rows = System.Array.Empty<SquadRowView>();

    public int VisibleRows
    {
        get
        {
            int count = 0;
            foreach (var row in m_rows)
            {
                if (row.gameObject.activeSelf)
                {
                    count++;
                }
            }

            return count;
        }
    }

    public SquadRowView Row(int index)
    {
        return index >= 0 && index < m_rows.Length ? m_rows[index] : null;
    }

    private void Update()
    {
        var players = NetworkPlayer.All;
        for (int i = 0; i < m_rows.Length; i++)
        {
            var player = i < players.Count ? players[i] : null;
            m_rows[i].Apply(player, player != null && player == Player);
        }
    }
}
}
