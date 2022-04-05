using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Core.UI
{
    public class UIJSAMObject<T> where T: IJSAMObject
    {
        private string path;

        private T jSAMObject;

        private bool modified;

        public event EventHandler Opening;
        public event EventHandler Opened;

        public event EventHandler Saving;
        public event EventHandler Saved;

        public event EventHandler Closing;
        public event EventHandler Closed;

        public event EventHandler Modified;

        public UIJSAMObject(string path)
        {
            this.path = path;
            modified = false;
        }

        public UIJSAMObject(T jSAMObject)
        {
            this.jSAMObject = jSAMObject;
            modified = false;
        }

        public UIJSAMObject()
        {
            
        }


        public string Path
        {
            get
            {
                return path;
            }
            set
            {
                path = value;
                modified = true;
            }
        }

        public T JSAMObject
        {
            get
            {
                if(jSAMObject == null)
                {
                    return default;
                }

                return Core.Query.Clone(jSAMObject);
            }

            set
            {
                jSAMObject = value;
                modified = true;
                OnModified();
            }
        }


        public bool Open()
        {
            OnOpening();

            bool result = false;
            if(!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
            {
                List<T> jSAMObjects = null;
                try
                {
                    jSAMObjects = Convert.ToSAM<T>(path);
                }
                catch(Exception exception)
                {
                    return false;
                }

                if(jSAMObjects != null && jSAMObjects.Count != 0)
                {
                    jSAMObject = jSAMObjects.FirstOrDefault();
                    result = jSAMObject != null;
                }
            }

            if(result)
            {
                OnOpened();
                modified = false;
            }

            return result;
        }

        public void OnOpening()
        {
            EventHandler eventHandler = Opening;
            if (eventHandler != null)
            {
                eventHandler(this, EventArgs.Empty);
            }
        }

        public void OnOpened()
        {
            EventHandler eventHandler;

            eventHandler = Opened;
            if (eventHandler != null)
            {
                eventHandler(this, EventArgs.Empty);
            }
        }


        public bool Close()
        {
            OnClosing();

            if (modified && jSAMObject != null)
            {
                DialogResult dialogResult = MessageBox.Show("Do you want to save before closing?", "Save", MessageBoxButtons.YesNoCancel);
                if(dialogResult == DialogResult.Cancel)
                {
                    return false;
                }

                if(dialogResult == DialogResult.Yes)
                {
                    bool result = Save();
                    if(!result)
                    {
                        return false;
                    }
                }
            }

            jSAMObject = default;

            modified = false;
            OnClosed();

            return true;
        }

        public void OnClosing()
        {
            EventHandler eventHandler = Closing;
            if (eventHandler != null)
            {
                eventHandler(this, EventArgs.Empty);
            }
        }

        public void OnClosed()
        {
            EventHandler eventHandler;

            eventHandler = Closed;
            if (eventHandler != null)
            {
                eventHandler(this, EventArgs.Empty);
            }
        }


        public bool Save()
        {
            OnSaving();

            if(jSAMObject == null)
            {
                return false;
            }

            if(string.IsNullOrWhiteSpace(path))
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "json files (*.json)|*.json|All files (*.*)|*.*";
                    saveFileDialog.FilterIndex = 1;
                    saveFileDialog.RestoreDirectory = true;

                    if (saveFileDialog.ShowDialog() != DialogResult.OK)
                    {
                        return false;
                    }

                    path = saveFileDialog.FileName;
                }
            }

            bool result = Convert.ToFile(new IJSAMObject[] { jSAMObject }, path);
            if(!result)
            {
                return result;
            }

            OnSaved();

            return result;
        }

        public void OnSaving()
        {
            EventHandler eventHandler = Saving;
            if (eventHandler != null)
            {
                eventHandler(this, EventArgs.Empty);
            }
        }

        public void OnSaved()
        {
            EventHandler eventHandler = Saved;
            if (eventHandler != null)
            {
                eventHandler(this, EventArgs.Empty);
            }
        }


        public void OnModified()
        {
            EventHandler eventHandler = Modified;
            if (eventHandler != null)
            {
                eventHandler(this, EventArgs.Empty);
            }
        }

    }
}
