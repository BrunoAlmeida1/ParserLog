using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using ParserLog.Models;

//Lista que irá receber os dados de cada uma das partidas
List<Games> games = new List<Games>();

//Variável que organiza e comenta os dados do log
string commentedData = "";

int gameId = 0;
//Variável que receberá os dados do log
StringBuilder gameData = new StringBuilder();

//Método que ler e armazena os dados de login dos jogadores e das kills contidos no log. Sempre que chega ao final dos dados de uma partida (ShutdownGame) ele realiza uma análise, dentro da condição ELSE, dos dados armazenados.
foreach (string line in System.IO.File.ReadLines("./Files/Inputs/Quake.txt"))
{
    if (!line.Contains("ShutdownGame:"))
    {
        if (line.Contains("InitGame:"))
            commentedData += "\n" + line + "\n* Inicia a partida.";

        else if (line.Contains("ClientUserinfoChanged:"))
            gameData.AppendLine(line);

        else if (line.Contains("Kill:"))
            gameData.AppendLine(line);

        else if (line.Contains("ClientDisconnect:"))
            gameData.AppendLine(line);
    }
    else
    {
        gameId++;
        //userIdList armazena o id do jogador e o seus dados num objeto da classe Player. O userIdList_2 é usado quando o log registra a desconexão de um jogador. Quando isso acontece, adicionamos o jogador desconectado à userIdList_2 e removemos de userIdList para que se um novo jogador se conectar usando o mesmo id de um antigo jogador ele não seja registrado como um novo nome dele. 
        Dictionary<string, Player> userIdList = new Dictionary<string, Player>();
        Dictionary<string, Player> userIdList_2 = new Dictionary<string, Player>();
        Dictionary<string, Player> userIdListTemp = new Dictionary<string, Player>();
        List<Player> playerList = new List<Player>();
        StatusGame status = new StatusGame();
        Games game = new Games();
        game.Game = gameId;
        int totalKills = 0;

        string stringData = gameData.ToString();
        StringReader reader = new StringReader(stringData);
        string lineData;

        while ((lineData = reader.ReadLine()) != null)
        {
            //Essa condição é acionada sempre que um novo dado de um usuário aparece no log. Em posse desses dados é adicionado novos jogadores a lista de usuários (userIdList) com o seus respectivos nicknames, novos e antigos. 
            if (lineData.Contains("ClientUserinfoChanged:"))
            {
                int startName = lineData.IndexOf("n\\");
                int endName = lineData.IndexOf("\\t");
                int NameLength = (endName - 1) - (startName + 1);

                string name = lineData.Substring(startName + 2, NameLength);

                int startID = lineData.IndexOf("d:");
                string userId = lineData.Substring(startID + 3, 1);
                Player user = new Player() { OldNames = new List<string>() };

                //Essa condição verifica se o id do usuário já está entre as Keys do dicionário userIdList. Senão estiver é analisado se o nome do Player já está entre os objetos do userIdList_2, se estiver acrescentamos um objeto Player com esse nome no userIdListTemp, senão é adicionado o usuário a userIdList. 
                //Mas se o id estiver ele vai para o Else e então verifica tanto na userIdList quanto na userIdListTemp se o atributo name do objeto armazenado na key correspondente ao id é diferente do novo nome apresentado. Se for ele atualiza o nome de usuário no dicionário em que esta condição é verdadeira. Se for na userIdList, é armazenado o antigo nome no atributo OldNames do objeto correspondente a ele; se for na userIdListTemp, atualizamos os atributos Name e OldNames de userIdList_2.
                if (!userIdList.ContainsKey(userId) && !userIdListTemp.ContainsKey(userId))
                {
                    if (userIdList_2.Count() > 0)
                    {
                        bool addUser = true;
                        foreach (var item in userIdList_2)
                        {
                            if (item.Value.Name.Equals(name))
                            {
                                addUser = false;
                                break;
                            }
                        }
                        if (addUser)
                        {
                            user.Name = name;
                            userIdList.Add(userId, user);
                        }
                        else
                        {
                            user.Name = name;
                            userIdListTemp.Add(userId, user);
                        }
                    }
                    else
                    {
                        user.Name = name;
                        userIdList.Add(userId, user);
                    }
                    commentedData += ($"\n{lineData} \n* O jogador {name} entrou na partida.");
                }
                else if (userIdList.ContainsKey(userId))
                {
                    commentedData += ($"\n{lineData} \n* O jogador {userIdList[userId].Name} mudou o nome para {name}.");

                    if (userIdList[userId].Name != name)
                    {
                        string oldName = userIdList[userId].Name;

                        userIdList[userId].OldNames.Add(oldName);
                        userIdList[userId].Name = name;
                    }
                }
                else if (userIdListTemp.ContainsKey(userId))
                {
                    if (userIdListTemp[userId].Name != name)
                    {
                        string oldName = userIdListTemp[userId].Name;

                        string key = userIdList_2.FirstOrDefault(x => x.Value.Name == oldName).Key;

                        userIdList_2[key].OldNames.Add(oldName);
                        userIdList_2[key].Name = name;
                        userIdListTemp[userId].Name = name;
                    }
                }
            }
            //Calcula o número de kills de cada jogador e o total da partida.
            else if (lineData.Contains("Kill:"))
            {
                totalKills++;

                int startKillerName = lineData.IndexOf(":", 16);
                int endKillerName = lineData.IndexOf("killed", startKillerName + 2);
                int killerNameLength = (endKillerName - 1) - (startKillerName + 2);
                string killerName = lineData.Substring(startKillerName + 2, killerNameLength);

                commentedData += "\n" + CommentKillData(lineData, killerName);

                if (killerName != "<world>")
                {
                    foreach (var item in userIdList)
                    {
                        if (item.Value.Name == killerName)
                            item.Value.Kills++;
                    }

                    foreach (var item in userIdList_2)
                    {
                        if (item.Value.Name == killerName)
                            item.Value.Kills++;
                    }
                }
            }
            //Remove um jogador que se desconectou da userIdList e o põe na userIdList_2.
            else if (lineData.Contains("ClientDisconnect:"))
            {
                int startID = lineData.IndexOf("t:");
                string userId = lineData.Substring(startID + 3);

                //Analisa se o objeto da classe Player (VALUE) e o id (KEY) já estão registrados no userIdList_2. Se o objeto estiver, não faz nada. Se o id estiver e o objeto não, ele trata o id para que um novo possa ser usado no lugar.
                if (userIdList.ContainsKey(userId))
                {
                    if (!userIdList_2.ContainsValue(userIdList[userId]))
                    {
                        if (!userIdList_2.ContainsKey(userId))
                        {
                            userIdList_2.Add(userId, userIdList[userId]);
                        }
                        else
                        {
                            bool searchId = true;
                            int id = Int32.Parse(userId);

                            while (searchId)
                            {
                                id++;
                                if (!userIdList_2.ContainsKey(id.ToString()))
                                {
                                    userIdList_2.Add(id.ToString(), userIdList[userId]);
                                    searchId = false;
                                }
                            }
                        }
                    }
                }
                userIdList.Remove(userId);
                userIdListTemp.Remove(userId);

            }
        }

        if (line.Contains("ShutdownGame:"))
            commentedData += "\n" + line + "\n* Termina a partida.";

        foreach (var item in userIdList)
        {
            playerList.Add(item.Value);
        }

        foreach (var item in userIdList_2)
        {
            playerList.Add(item.Value);
        }

        status.Players = playerList;
        status.totalKills = totalKills;
        game.Status = status;
        games.Add(game);

        gameData.Clear();
    }
}

string finalData = JsonConvert.SerializeObject(games, Formatting.Indented);

File.WriteAllText("./Files/Outputs/log.json", finalData);
File.WriteAllText("./Files/Outputs/Dados_organizados_do_log.txt", commentedData);


string CommentKillData(string line, string killer)
{
    int startDeadName = line.IndexOf("killed ");
    int endDeadName = line.IndexOf("by");
    int deadNameLength = (endDeadName - 1) - (startDeadName + 6);
    string deadName = line.Substring(startDeadName + 6, deadNameLength);

    int startGunName = line.IndexOf("D_");
    string gunName = line.Substring(startGunName + 2);

    TextInfo ti = CultureInfo.CurrentCulture.TextInfo;
    gunName = ti.ToTitleCase(gunName.ToLower()).Replace("_", " ");

    if (killer.Equals("<world>"))
    {
        if (gunName.Equals("Trigger Hurt"))
            return $"{line}\n* O player \"{deadName}\" morreu por que estava ferido e caiu de uma altura que o matou.";

        else if (gunName.Equals("Falling"))
            return $"{line}\n* O player \"{deadName}\" morreu por que caiu de uma altura que o matou.";

        else if (gunName.Equals("Crush"))
            return $"{line}\n* O player \"{deadName}\" morreu esmagado.";
    }

    return $"{line}\n* O player \"{killer}\" matou o player \"{deadName}\" usando a arma \"{gunName}\".";
}