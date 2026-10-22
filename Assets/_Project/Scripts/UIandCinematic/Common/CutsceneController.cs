namespace UIandCinematic
{
    /// <summary>
    /// 지금 씬에서 대화를 맡는 쪽(ICutsceneController)을 등록해 두는 자리. UIManager가 켜질 때 등록하고 꺼질 때 해제한다.
    /// CinematicManager·StageManager는 UIManager 대신 여기서 대화를 맡는 쪽을 찾는다.
    /// </summary>
    public static class CutsceneController
    {
        // 지금 등록된 대화를 맡는 쪽. 없으면 null이다.
        public static ICutsceneController Current { get; private set; }

        public static void Register(ICutsceneController controller)
        {
            Current = controller;
        }

        // 등록된 쪽이 controller일 때만 비운다. 다른 쪽이 나중에 등록했으면 그대로 둔다.
        public static void Unregister(ICutsceneController controller)
        {
            if (Current == controller)
                Current = null;
        }
    }
}
