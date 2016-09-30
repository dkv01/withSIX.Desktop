using System;
using withSIX.Core;
using withSIX.Core.Helpers;
using withSIX.Sync.Core.Legacy.Status;

namespace withSIX.Play.Core.Games.Services
{
    public class StatusMod : ModelBase, IHaveTimestamps
    {
        ActionState _action;
        bool _completed;
        string _name = string.Empty;
        StatusRepo _repo;
        TimeSpan? _timeTaken;

        StatusMod() {
            CreatedAt = Tools.Generic.GetCurrentUtcDateTime;
            UpdatedAt = Tools.Generic.GetCurrentUtcDateTime;
        }

        public StatusMod(string mod) : this() {
            _name = mod;
        }

        public void Abort() => _repo?.Abort();

        #region StatusMod Members

        public TimeSpan? TimeTaken
        {
            get { return _timeTaken; }
            set { SetProperty(ref _timeTaken, value); }
        }

        public StatusRepo Repo
        {
            get { return _repo; }
            set { SetProperty(ref _repo, value); }
        }

        public bool Completed
        {
            get { return _completed; }
            set
            {
                UpdateTimeTaken();
                CalcFileSizes();
                var repo = Repo;
                if (repo != null)
                    repo.Finish();
                Action = ActionState.Ready;
                SetProperty(ref _completed, value);
            }
        }

        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        public ActionState Action
        {
            get { return _action; }
            set { SetProperty(ref _action, value); }
        }

        public void UpdateStamp() {
            UpdatedAt = Tools.Generic.GetCurrentUtcDateTime;
        }

        public void UpdateTimeTaken() {
            UpdateStamp();
            TimeTaken = UpdatedAt - CreatedAt;
        }

        public void CalcFileSizes() {
            var repo = Repo;
            if (repo != null)
                repo.CalcFileSizes();
        }

        #endregion
    }
}