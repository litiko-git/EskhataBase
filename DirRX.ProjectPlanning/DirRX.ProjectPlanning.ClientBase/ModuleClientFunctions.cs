using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ProjectPlanning.Client
{
  public class ModuleFunctions
  {
    
    /// <summary>
    /// Создать диалог подтверждения действия.
    /// </summary>
    /// <param name="dialogMessage">Сообщение диалога.</param>
    /// <param name="dialogDescription">Описание диалога.</param>
    /// <returns>Диалог.</returns>
    [Public]
    public CommonLibrary.ITaskDialog CreateConfirmDialog(string dialogMessage, string dialogDescription)
    {
      var dialog = Dialogs.CreateTaskDialog(dialogMessage, dialogDescription, MessageType.Question);
      dialog.Buttons.AddYesNo();
      dialog.Buttons.Default = DialogButtons.Yes;
      
      return dialog;
    }

  }
}