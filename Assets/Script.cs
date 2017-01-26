using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class Script : MonoBehaviour 
{
	public Text targetText, scoreText, disableNextText, enableNextText;
	public GameObject comingChallengeGO;
	public Rigidbody ball;
	string userdata, gamedata, gamedataro;
	Progress challenges;
	DateTime nextChallenge;
	int currentChallenge = -1, score = 0;
	float comingChallenge = -1;
	bool isGameActive = false;

	void Start () 
	{
		userdata = Application.persistentDataPath + "/usr.dat";
		gamedata = Application.persistentDataPath + "/gam.dat";
		gamedataro = Application.streamingAssetsPath + "/challenge.json";
		LoadProgress();
	}

	void Update () 
	{
		if(Input.GetKeyDown(KeyCode.R))
			Reset();
		if(Input.GetKeyDown(KeyCode.Space))
			Jump();
		if(comingChallenge > 0 && comingChallenge < Time.time)
			NewChallenge();
		if(!isGameActive)
			disableNextText.text = "Next challenge in : " + ((int)(comingChallenge - Time.time)/3600).ToString("00") + ":" + (((int)(comingChallenge - Time.time)/60)%60).ToString("00") + ":" + ((int)(comingChallenge - Time.time)%60).ToString("0") + "s";
		else
			enableNextText.text = "Next challenge in : " + ((int)(comingChallenge - Time.time)/3600).ToString("00") + ":" + (((int)(comingChallenge - Time.time)/60)%60).ToString("00") + ":" + ((int)(comingChallenge - Time.time)%60).ToString("0") + "s";
	}

	void Jump()
	{
		if(challenges.list[currentChallenge].isComplete)
			return;
		score++;
		scoreText.text = score.ToString();
		ball.AddForce(Vector3.up * 300);
		if(score >= challenges.list[currentChallenge].challenge)
			ChallengeComplete();
	}

	void ChallengeComplete(){
		challenges.lastChallenge = DateTime.UtcNow.ToString();
		challenges.twoHourChallengeGiven = false;
		challenges.list[currentChallenge].isComplete = true;
		SaveProgress();
		comingChallenge = Time.unscaledTime + 7200;
		DisableGame();
	}

	void DisableGame()
	{
		comingChallengeGO.SetActive(true);
		enableNextText.enabled = false;
		isGameActive = false;
	}

	void EnableGame()
	{
		scoreText.text = score.ToString();
		targetText.text = challenges.list[currentChallenge].challenge.ToString();
		comingChallengeGO.SetActive(false);
		enableNextText.enabled = true;
		isGameActive = true;
	}

	void Reset(){
		currentChallenge = -1;
		File.Delete(gamedata);
		LoadProgress();
	}

	void NewChallenge()
	{
		int t = UnityEngine.Random.Range(0, challenges.list.Length);
		if(challenges.list[t].isComplete){
			float f = t = (t+1)%challenges.list.Length;
			while(challenges.list[t].isComplete && t != f)
				t = (t+1)%challenges.list.Length;
			if(t == f)
				Reset();
		}
		currentChallenge = challenges.currentChallengeID = t;
		challenges.lastChallenge = DateTime.UtcNow.ToString();
		SaveProgress();
		score = 0;
		scoreText.text = score.ToString();
		targetText.text = challenges.list[currentChallenge].challenge.ToString();
		EnableGame();
	}

	void LoadProgress()
	{
		if(!File.Exists(gamedata)){
			string s = File.ReadAllText(gamedataro);
			challenges = JsonUtility.FromJson<Progress>(s);
		}
		else{
			using(Stream s = File.Open(gamedata, FileMode.Open)){
				BinaryFormatter b = new BinaryFormatter();
				challenges = b.Deserialize(s) as Progress;
			}
		}
		if(challenges.currentChallengeID <= 0){
			NewChallenge();
			/*currentChallenge = UnityEngine.Random.Range(0, challenges.list.Length);
			challenges.currentChallengeID = challenges.list[currentChallenge].id;
			challenges.lastChallenge = DateTime.UtcNow.ToString();
			EnableGame();*/
			SaveProgress();
		}
		else{
			for(int i=0;i<challenges.list.Length;i++)
				if(challenges.list[i].id == challenges.currentChallengeID)
					currentChallenge = i;
			if(challenges.list[currentChallenge].isComplete){
				TimeSpan t = DateTime.UtcNow.Subtract(DateTime.Parse(challenges.lastChallenge));
				if(t.TotalSeconds >= 7200)
					NewChallenge();
				else{
					comingChallenge = Time.time + 7200 - (float)t.TotalSeconds;
					DisableGame();
				}
			}
			else{
				TimeSpan t = DateTime.UtcNow.Subtract(DateTime.Parse(challenges.lastChallenge));
				if(t.TotalHours >= 24)
					NewChallenge();
				else
					comingChallenge = Time.time + 24 * 3600 - (float)t.TotalSeconds;
				EnableGame();
			}
			nextChallenge = DateTime.Parse(challenges.lastChallenge).AddHours(challenges.twoHourChallengeGiven ? 24 : 2);
		}
	}

	void SaveProgress(){
		if(File.Exists(gamedata))
			File.Delete(gamedata);
		using(Stream s = File.Open(gamedata, FileMode.Create)){
			BinaryFormatter b = new BinaryFormatter();
			b.Serialize(s, challenges);
		}
	}
}


[Serializable]
class ChallengeDetail{
	public int id, challenge, reward;
	public string challenge_detail, reward_detail;
	public bool isComplete = false;
}

[Serializable]
class Progress{
	public int coins, currentChallengeID;
	public string lastChallenge;
	public bool twoHourChallengeGiven;
	public ChallengeDetail[] list;
}