using HiveAPI.CS;               //Used for Hive Blockchain Interaction available at ("https://gitlab.syncad.com/hive/hive-net")
using Newtonsoft.Json;          //Used to Decode Web API Data
using System;                   //Used for Console
using System.IO;                //Used for Reading and Writing txt files
using System.Net;               //Used for WebClient
using System.Net.Http;          //Used for WebClient
using System.Threading.Tasks;   //Used for Delays

namespace GLX_Stacker
{
    internal class Program
    {
        /*
        Program summary.
        This program posts to the hive blockchain to claim your GLX tokens.
        Then it reads the GLX web API to determine the amount of tokens you have.
        If your GLX unstaked tokens are > 5 then it will stake ALL of your GLX tokens.
     
        Note: I am in no way responsible for your account or keys.
        It is up to you to understand this program and protect your keys from foul use.        
        Treat this program as you would your keys.
        
        Note2: balance.txt is needed in the same folder as the executible.  This text file should contain a single number (no comma)
        this number is used to determine the number of tokens to exclude from staking and if you select a stake percent then
        it will increase as more tokens are staked.  Anything other than a number will crash the program.

        */

        private static string hiveUser = "your.username"; //lower case with no @
        private static string activeKey = "your active key goes here";  //GLX staking needs your active key
        private static string postingKey = "your posting key goes here";  //not needed for GLX restaking but you may choose to expand the code to do more. 
        private const double restake_amt = 5; //number of tokens before we stake them all. 
        private const int wait_length = 300; //number of seconds between attempts (dont set this too low)
        private static WebClient wc = new WebClient();  //This is the connection for the GLX web API
        private static string response; //This stores the response we get from the web API
        private static bool failed_download = false; //This is true when we fail to get the response        
        private static double GLX = 0; //GLX stoarage value
        private static double GLXP = 0; //GLXP storage value
        private static double No_Stake = 0; //How many tokens we save from staking.
        private static double Stake_Pcnt = 0.80; //What % of the tokens are restaked (if you have 100 tokens and this is 0.80 then it will stake 80 of them) 

        static async Task Main(string[] args)
        {

            get_value(); //Gets the stored value from balance.txt

            //This section allows you to use the compiled program without storing your keys in it.            
            if (hiveUser == "your.username")
            {
                Console.ForegroundColor = ConsoleColor.White; //Color the console because we can
                Console.WriteLine(" ============================================================================");
                Console.WriteLine("  GLX Stacking Program");
                Console.WriteLine("  Written by chaoscommander");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  If you like it please consider sending me some GLX.");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  Use at own risk!");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write("  " + No_Stake);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("  GLX tokens are excluded from staking!");
                Console.WriteLine();
                Console.WriteLine("  Keys are not stored in program.  You will need to enter them.");
                Console.WriteLine("  Keys will not be stored except in memory.");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  Once program is closed the Keys are lost!");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(" ============================================================================");
                Console.WriteLine();
                Console.Write("  Enter hive Username : ");
                hiveUser = Console.ReadLine(); //Read in data from the console
                Console.Write("  Enter Active Key : ");
                activeKey = Console.ReadLine();
                Console.Write("  % to Stake example 0.80 : ");
                Stake_Pcnt = double.Parse(Console.ReadLine());
                Console.Clear();
            }

            Console.CursorVisible = false; //hide the cursor
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" ============================================================================");
            Console.WriteLine("  GLX Stacking Program");
            Console.WriteLine("  Written by chaoscommander");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  If you like it please consider sending me some GLX. ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  Use at own risk!");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("  This program will stake ");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write((Stake_Pcnt * 100) + "%");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(" of GLX tokens every ");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write((wait_length / 60));
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" Min!");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("  " + No_Stake);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" GLX tokens are excluded from staking!");
            Console.WriteLine(" ============================================================================");
            Console.WriteLine();
            await Task.Delay(10000); //Delay to give users a chance to close program without making a transaction                   


            do //everything in these { } gets done over and over in order.
            {
                //Claim GLX Rewards
                hive_custom_json("gls-plat-stake_tokens", "{\"token\":\"GLX\",\"to_player\":\"" + hiveUser + "\",\"qty\":0}", false);

                //10 sec delay to make sure block is processed
                await Task.Delay(10000);

                //Check GLX API to see how many tokens we have.
                glx_get_tokens(hiveUser);

                //if we have more than 5 tokens we restake (this is done to make sure we dont needlessly waste RC)
                if ((GLX - No_Stake) >= restake_amt)
                {
                    double stake = ((GLX - No_Stake) * Stake_Pcnt);
                    double save = (No_Stake + ((GLX - No_Stake) * (1 - Stake_Pcnt)));

                    stake = Math.Round(stake, 4); //Round values to prevent long floats
                    save = Math.Round(save, 4);

                    //Stake GLX Rewards
                    hive_custom_json("gls-plat-stake_tokens", "{\"token\":\"GLX\",\"qty\":" + stake  + "}", false);
                    Console.WriteLine(" Total Staked : " + GLXP + " | Adding : " + stake + " GLX | Balance : " + save + " | Time : " + System.DateTime.Now.ToString());
                    No_Stake = save;  //Adjust the staking value to include what we just staked.
                    write_value(No_Stake.ToString()); //Save to balance.txt

                    //Write to console total staked & amount added
                    
                }
                else
                {
                    //Restake amount was not reached.  Let the user know we are not staking anything.
                    Console.WriteLine(" Total Staked : " + GLXP + " | Adding : 0 GLX | Balance : " + GLX + " | Time : " + System.DateTime.Now.ToString());
                }

                //This causes the program to wait x seconds where x = wait_length
                await Task.Delay(1000 * (wait_length - 10));

            } while (true);
        }
        static void write_value(string value)
        {
            string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string strWorkPath = System.IO.Path.GetDirectoryName(strExeFilePath);
            using (StreamWriter writer = new StreamWriter(strWorkPath + @"\balance.txt"))
            {
                writer.WriteLine(value);
            }
        }

        static void get_value()
        {
            string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string strWorkPath = System.IO.Path.GetDirectoryName(strExeFilePath);
            string readText = File.ReadAllText(strWorkPath + @"\balance.txt");
            No_Stake = double.Parse(readText);
        }

        static dynamic jsonParse(string response)
        {
            //This function converts the web api to a machine readable data.
            try { var values = JsonConvert.DeserializeObject<dynamic>(response); return values; }
            catch (Exception) { return null; }
        }

        private static void glx_get_tokens(string user)
        {
            //This function polls the GLX API for the users staked and liquid tokens and stores them in memory.
            try { response = wc.DownloadString("https://validator.genesisleaguesports.com/balances?account=" + user); failed_download = false; }
            catch (Exception) { Console.WriteLine(" API Failed"); failed_download = true; }
            if (failed_download != true)
            {
                var values = jsonParse(response); //We take the string from the api and turn it into a json object.
                for (int i = 0; i < values.Count; i++) // We loop through every item in the object looking for the values we need. 
                {
                    if (values[i]["token"].ToString() == "GLX")
                    {
                        GLX = double.Parse(values[i]["balance"].ToString());
                    }
                    if (values[i]["token"].ToString() == "GLXP")
                    {
                        GLXP = double.Parse(values[i]["balance"].ToString());
                    }
                }
            }
            else
            {
                //If the download fails then we clear the token values
                GLX = 0;
                GLXP = 0;
            }
        }

        private static string hive_custom_json(string id, string json, bool auth_posting)
        {
            //This function posts custom_json's to the blockchain.
            HttpClient oHTTP = new HttpClient();
            CHived oHived = new CHived(oHTTP, "https://anyx.io");

            if (auth_posting == true)
            {
                //Posting key is required (this isnt used and is here for expandability)
                COperations.custom_json custom_json = new COperations.custom_json
                {
                    id = id,
                    json = json,
                    required_auths = new string[] { },
                    required_posting_auths = new string[] { hiveUser.ToLower() },
                };
                try
                {
                    string txid = oHived.broadcast_transaction(new object[] { custom_json }, new string[] { postingKey });
                    return txid;
                }
                catch (Exception e)
                {
                    return e.Message;
                    Console.WriteLine("Error");
                }
            }
            else
            {
                //Active key is required
                COperations.custom_json custom_json = new COperations.custom_json
                {
                    id = id,
                    json = json,
                    required_auths = new string[] { hiveUser.ToLower() },
                    required_posting_auths = new string[] { },
                };
                try
                {
                    string txid = oHived.broadcast_transaction(new object[] { custom_json }, new string[] { activeKey });
                    return txid;
                }
                catch (Exception e)
                {
                    return e.Message;
                    Console.WriteLine("Error");
                }
            }
        }
    }
}
