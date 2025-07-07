namespace AgentAI.Domain;

public static class ChatModeConstants
{
    public static class Constants
    {
        public const string SelectAgent = "[AI] คุยกับ Agent AI โดยขึ้น Session ใหม่";  // คุยกับ Agent AI ขึ้น Session ใหม่
        public const string SelectHumanAdmin = "[Human] คุยกับ Admin (คน)";        // คุยกับ Admin (คน)
    }
    public enum Enums
    {
        ContinuteChat = 0, // คุยต่อใน Session เดิม
        SelectAgent = 2,  // เลือก Agent AI โดยขึ้น Session ใหม่
        SelectHumanAdmin = 3,  // เลือกคุยกับ Admin (คน) ใน Session เดิม
    }
}