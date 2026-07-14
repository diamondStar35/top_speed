using Key = TopSpeed.Input.InputKey;
using TopSpeed.Drive.Single;
using TopSpeed.Drive;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private bool _pitMenuShown;

        private void RunTimeTrial(float elapsed)
        {
            if (_timeTrial == null)
            {
                EndRace();
                return;
            }

            _timeTrial.Run(elapsed);
            if (_timeTrial.WantsPause)
                EnterPause(AppState.TimeTrial);
            if (_timeTrial.WantsExit || _input.WasPressed(Key.Escape) || ConsumeDriveTouchExitRequest())
            {
                EndRace(_timeTrial.WantsExit ? _timeTrial.ConsumeResultSummary() : null);
                return;
            }
            if (_timeTrial.WantsPitStopMenu && !_pitMenuShown)
            {
                _pitMenuShown = true;
                _choices.Show(PitStopMenu.Create(result =>
                {
                    _pitMenuShown = false;
                    if (!result.IsCanceled)
                        _timeTrial?.AcceptPitStopChoice(result.ChoiceId);
                }));
            }
            if (_choices.HasActiveChoiceDialog)
            {
                var action = _menu.Update(_input);
                HandleMenuAction(action);
            }
        }

        private void RunSingleRace(float elapsed)
        {
            if (_singleRace == null)
            {
                EndRace();
                return;
            }

            _singleRace.Run(elapsed);
            if (_singleRace.WantsPause)
                EnterPause(AppState.SingleRace);
            if (_singleRace.WantsExit || _input.WasPressed(Key.Escape) || ConsumeDriveTouchExitRequest())
            {
                EndRace(_singleRace.WantsExit ? _singleRace.ConsumeResultSummary() : null);
                return;
            }
            if (_singleRace.WantsPitStopMenu && !_pitMenuShown)
            {
                _pitMenuShown = true;
                _choices.Show(PitStopMenu.Create(result =>
                {
                    _pitMenuShown = false;
                    if (!result.IsCanceled)
                        _singleRace?.AcceptPitStopChoice(result.ChoiceId);
                }));
            }
            if (_choices.HasActiveChoiceDialog)
            {
                var action = _menu.Update(_input);
                HandleMenuAction(action);
            }
        }

        private void EndRace(DriveResultSummary? resultSummary = null)
        {
            _timeTrial?.FinalizeSession();
            _timeTrial?.Dispose();
            _timeTrial = null;

            _singleRace?.FinalizeSession();
            _singleRace?.Dispose();
            _singleRace = null;

            _pitMenuShown = false;
            _input.ResetState();
            _state = AppState.Menu;
            _menu.ShowRoot("main");
            _menu.FadeInMenuMusic();
            if (resultSummary != null)
                ShowRaceResultDialog(resultSummary);
        }
    }
}





