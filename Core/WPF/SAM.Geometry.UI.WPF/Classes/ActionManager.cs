using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public class ActionManager
    {
        List<IAction> actions;

        public ActionManager()
        {
            actions = new List<IAction>();
        }

        public List<T> GetActions<T>() where T:IAction
        {
            IEnumerable<T> ts = actions?.OfType<T>();
            if(ts == null)
            {
                return null;
            }

            return ts.ToList();
        }

        public T GetAction<T>() where T: IAction
        {
            if(actions == null || actions.Count == 0)
            {
                return default(T);
            }

            return actions.OfType<T>().FirstOrDefault();
        }


        public bool Update(IAction action)
        {
            if (action is HighlightAction)
            {
                Update_Highlight(action as dynamic);
                return true;
            }
            else if (action is SelectAction)
            {
                Update_Select(action as dynamic);
                return true;
            }

            return false;
        }

        public bool Apply(IAction action)
        {
            if (action is HighlightAction)
            {
                Apply_Highlight(action as dynamic);
                return true;
            }
            else if (action is SelectAction)
            {
                Apply_Select(action as dynamic);
                return true;
            }

            return false;
        }

        public bool Cancel<T>() where T : IAction
        {
            return Cancel(typeof(T));
        }

        private bool Cancel(System.Type type)
        {
            if (type == typeof(HighlightAction))
            {
                Cancel_Highlight();
                return true;
            }
            else if (type == typeof(SelectAction))
            {
                Cancel_Select();
                return true;
            }

            return false;
        }

        public bool Cancel()
        {
            List<System.Type> types = actions?.ConvertAll(x => x.GetType());
            if (types == null || types.Count == 0)
            {
                return true;
            }

            bool result = true;
            foreach (System.Type type in types.Distinct())
            {
                if (!Cancel(type))
                {
                    result = false;
                }
            }

            return result;
        }


        private void Add(IAction action)
        {
            if(action == null)
            {
                return;
            }

            if(actions == null)
            {
                actions = new List<IAction>();
            }

            actions.Add(action);
        }

        private void Update_Highlight(HighlightAction highlightAction)
        {
            List<Visual3D> visual3Ds = new List<Visual3D>();

            List<Visual3D> visual3Ds_Temp = highlightAction?.Visual3Ds;
            if(visual3Ds_Temp != null)
            {
                foreach (Visual3D visual3D in visual3Ds_Temp)
                {
                    if (!visual3Ds.Contains(visual3D))
                    {
                        visual3Ds.Add(visual3D);
                    }
                }
            }

            IEnumerable<HighlightAction> highlightActions = GetActions<HighlightAction>();
            if (highlightActions != null)
            {
                foreach (HighlightAction highlightAction_Temp in highlightActions)
                {
                    visual3Ds_Temp = highlightAction_Temp.Visual3Ds;
                    actions.Remove(highlightAction_Temp);

                    if(visual3Ds_Temp != null)
                    {
                        foreach(Visual3D visual3D in visual3Ds_Temp)
                        {
                            if(!visual3Ds.Contains(visual3D))
                            {
                                visual3Ds.Add(visual3D);
                            }
                        }
                    }
                }
            }

            if(visual3Ds != null && visual3Ds.Count != 0)
            {
                HighlightAction highlightAction_Temp = new HighlightAction(visual3Ds);
                highlightAction_Temp.Highlight(true);
                actions.Add(highlightAction_Temp);
            }
        }

        private void Apply_Highlight(HighlightAction highlightAction)
        {
            Cancel_Highlight();

            highlightAction.Highlight(true);
            actions.Add(highlightAction);
        }

        private void Cancel_Highlight()
        {
            bool cancelled = false;
            IEnumerable<HighlightAction> highlightActions = GetActions<HighlightAction>();
            if (highlightActions != null)
            {
                foreach (HighlightAction highlightAction_Temp in highlightActions)
                {
                    cancelled = true;
                    highlightAction_Temp.Highlight(false);
                    actions.Remove(highlightAction_Temp);
                }
            }

            if(cancelled)
            {
                Update_Select(true);
            }
        }

        private void Update_Select(SelectAction selectAction)
        {
            List<Visual3D> visual3Ds = new List<Visual3D>();

            List<Visual3D> visual3Ds_Temp = selectAction?.Visual3Ds;
            if (visual3Ds_Temp != null)
            {
                foreach (Visual3D visual3D in visual3Ds_Temp)
                {
                    if (!visual3Ds.Contains(visual3D))
                    {
                        visual3Ds.Add(visual3D);
                    }
                }
            }

            IEnumerable<SelectAction> selectActions = GetActions<SelectAction>();
            if (selectActions != null)
            {
                foreach (SelectAction selectAction_Temp in selectActions)
                {
                    visual3Ds_Temp = selectAction_Temp.Visual3Ds;
                    actions.Remove(selectAction_Temp);

                    if (visual3Ds_Temp != null)
                    {
                        foreach (Visual3D visual3D in visual3Ds_Temp)
                        {
                            if (!visual3Ds.Contains(visual3D))
                            {
                                visual3Ds.Add(visual3D);
                            }
                        }
                    }
                }
            }

            Update_Select(true);
        }

        private void Update_Select(bool select = true)
        {
            List<Visual3D> visual3Ds = new List<Visual3D>();

            IEnumerable<SelectAction> selectActions = GetActions<SelectAction>();
            if (selectActions != null)
            {
                foreach (SelectAction selectAction_Temp in selectActions)
                {
                    List<Visual3D> visual3Ds_Temp = selectAction_Temp.Visual3Ds;
                    actions.Remove(selectAction_Temp);

                    if (visual3Ds_Temp != null)
                    {
                        foreach (Visual3D visual3D in visual3Ds_Temp)
                        {
                            if (!visual3Ds.Contains(visual3D))
                            {
                                visual3Ds.Add(visual3D);
                            }
                        }
                    }
                }
            }

            if (visual3Ds != null && visual3Ds.Count != 0)
            {
                SelectAction selectAction_Temp = new SelectAction(visual3Ds);
                selectAction_Temp.Select(select);
                actions.Add(selectAction_Temp);
            }
        }

        private void Apply_Select(SelectAction selectAction)
        {
            Cancel_Select();

            if(selectAction != null)
            {
                selectAction.Select(true);
                actions.Add(selectAction);
            }
        }

        private void Cancel_Select()
        {
            IEnumerable<SelectAction> selectActions = GetActions<SelectAction>();
            if (selectActions != null)
            {
                foreach (SelectAction selectAction_Temp in selectActions)
                {
                    selectAction_Temp.Select(false);
                    actions.Remove(selectAction_Temp);
                }
            }
        }

    }
}
