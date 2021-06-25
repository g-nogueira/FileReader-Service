; -- donate.iss --

[Files]
Source: "donate.bmp"; Flags: dontcopy

[CustomMessages]
DonateDonateHint=Support IOTLink - Thank you!

[Code]
procedure DonateImageOnClick(Sender: TObject);
var
  ErrorCode: Integer;
begin
  ShellExecAsOriginalUser('open', 'https://donorbox.org/iotlink', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
end;

<event('InitializeWizard')>
procedure DonateInitializeWizard;
var
  ImageFileName: String;
  DonateImage: TBitmapImage;
  BevelTop: Integer;
begin
  ImageFileName := ExpandConstant('{tmp}\donate.bmp');
  ExtractTemporaryFile(ExtractFileName(ImageFileName));

  DonateImage := TBitmapImage.Create(WizardForm);
  DonateImage.AutoSize := True;
  DonateImage.Bitmap.LoadFromFile(ImageFileName);
  DonateImage.Hint := CustomMessage('DonateDonateHint');
  DonateImage.ShowHint := True;
  DonateImage.Anchors := [akLeft, akBottom];
  BevelTop := WizardForm.Bevel.Top;
  DonateImage.Top := BevelTop + (WizardForm.ClientHeight - BevelTop - DonateImage.Bitmap.Height) div 2;
  DonateImage.Left := DonateImage.Top - BevelTop;
  DonateImage.Cursor := crHand;
  DonateImage.OnClick := @DonateImageOnClick;
  DonateImage.Parent := WizardForm;
end;